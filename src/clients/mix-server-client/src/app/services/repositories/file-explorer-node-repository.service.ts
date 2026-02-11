import {Injectable} from '@angular/core';
import {BehaviorSubject, filter, firstValueFrom, from, Observable} from "rxjs";
import {LoadingRepositoryService} from "./loading-repository.service";
import {FileExplorerFolderNode} from "../../main-content/file-explorer/models/file-explorer-folder-node";
import {ActivatedRoute, NavigationEnd, Router} from "@angular/router";
import {PageRoutes} from "../../page-routes.enum";
import {AuthenticationService} from "../auth/authentication.service";
import {FileExplorerFolderSortMode} from "../../main-content/file-explorer/enums/file-explorer-folder-sort-mode";
import {ServerConnectionState} from "../auth/enums/ServerConnectionState";
import {FileExplorerFolder} from "../../main-content/file-explorer/models/file-explorer-folder";
import {NodePathHeader, NodePath} from "../../main-content/file-explorer/models/node-path";
import {NodeApiService} from "../api.service";
import {FileExplorerNodeConverterService} from "../converters/file-explorer-node-converter.service";
import {NodePathConverterService} from "../converters/node-path-converter.service";
import {FolderSignalrClientService} from "../signalr/folder-signalr-client.service";
import {PlaybackDeviceRepositoryService} from "./playback-device-repository.service";
import {FileExplorerFileNode} from "../../main-content/file-explorer/models/file-explorer-file-node";
import {FileExplorerNode} from "../../main-content/file-explorer/models/file-explorer-node";
import {
  FolderSortMode,
  RefreshFolderCommand,
  SetFolderSortCommand
} from "../../generated-clients/mix-server-clients";

@Injectable({
  providedIn: 'root'
})
export class FileExplorerNodeRepositoryService {
  private _currentFolderPath$ = new BehaviorSubject<NodePathHeader | null>(null);
  private _currentFolder$ = new BehaviorSubject<FileExplorerFolder>(FileExplorerFolder.Default);

  private _loggedIn: boolean = false;

  constructor(private _authenticationService: AuthenticationService,
              private _fileExplorerNodeConverter: FileExplorerNodeConverterService,
              private _folderSignalRClient: FolderSignalrClientService,
              private _loadingRepository: LoadingRepositoryService,
              private _nodeClient: NodeApiService,
              private _nodePathConverter: NodePathConverterService,
              private _playbackDeviceService: PlaybackDeviceRepositoryService,
              private _route: ActivatedRoute,
              private _router: Router) {
    this._authenticationService.serverConnectionStatus$
      .subscribe(serverConnectionStatus => {
        this._loggedIn = serverConnectionStatus === ServerConnectionState.Connected;
        if (this._loggedIn) {
          this.loadDirectoryFromPathName(window.location.pathname);
        }
        else if (serverConnectionStatus === ServerConnectionState.Unauthorized) {
          this.clear();
        }
      })

    this._router.events
      .pipe(filter((e): e is NavigationEnd => e instanceof NavigationEnd))
      .subscribe(_ => {
        if (this._loggedIn) {
          this.loadDirectoryFromPathName(window.location.pathname);
        }
      });

    // Handle playback device changes - update canPlay flags
    this._playbackDeviceService.requestPlaybackDevice$
      .subscribe(device => {
        const folder = this._currentFolder$.value;
        if (folder.node.path.key === "") {
          return;
        }

        const nextFolder = folder.copy();
        nextFolder.children.forEach(node => {
          if (node instanceof FileExplorerFileNode) {
            node.updateCanPlay(device);
          }
        });
        this._currentFolder$.next(nextFolder);
      });

    // Handle SignalR folder refresh events
    this._folderSignalRClient.folderRefreshed$()
      .subscribe(updatedFolder => {
        if (this.isCurrentFolder(updatedFolder.node.path.key)) {
          this.updateCurrentFolder(updatedFolder);
        }
      });

    // Handle SignalR folder sorted events
    this._folderSignalRClient.folderSorted$()
      .subscribe(updatedFolder => {
        if (this.isCurrentFolder(updatedFolder.node.path.key)) {
          this.updateCurrentFolder(updatedFolder);
          this._loadingRepository.stopLoading(updatedFolder.node.path.key);
        }
      });

    // Handle SignalR node updated events
    this._folderSignalRClient.nodeUpdated$()
      .subscribe(event => {
        const node = event.node;
        if (!this.isCurrentFolder(node.parent?.path.key ?? "")) {
          return;
        }

        const nextFolder = this._currentFolder$.value.copy();
        const oldPathIndex = !!event.oldPath
          ? nextFolder.children.findIndex(n => n.path.isEqual(event.oldPath))
          : -1;

        const index = oldPathIndex === -1
          ? nextFolder.children.findIndex(n => n.path.isEqual(node.path))
          : oldPathIndex;

        this.insertNodeIntoFolder(nextFolder, node, index, event.index);
        this.updateCurrentFolder(nextFolder);
      });

    // Handle SignalR node deleted events
    this._folderSignalRClient.nodeDeleted$()
      .subscribe(event => {
        if (!this.isCurrentFolder(event.parent?.path.key ?? "")) {
          return;
        }

        const nextFolder = this._currentFolder$.value.copy();
        nextFolder.children = nextFolder.children.filter(f => !f.path.isEqual(event.nodePath));
        this.updateCurrentFolder(nextFolder);
      });

    // Handle SignalR media info updated events
    this._folderSignalRClient.mediaInfoUpdated$()
      .subscribe(event => {
        const currentFolderKey = this._currentFolder$.value.node.path.key;
        const relevantUpdates = event.mediaInfo.filter(item => item.nodePath.parent.key === currentFolderKey);

        if (relevantUpdates.length === 0) {
          return;
        }

        const nextFolder = this._currentFolder$.value.copy();
        relevantUpdates.forEach(update => {
          const node = nextFolder.children.find(n => n.path.fileName === update.nodePath.fileName);
          if (node instanceof FileExplorerFileNode) {
            node.metadata.mediaInfo = update.info;
          }
        });
        this.updateCurrentFolder(nextFolder);
      });

    // Handle SignalR media info removed events
    this._folderSignalRClient.mediaInfoRemoved$()
      .subscribe(event => {
        const currentFolderKey = this._currentFolder$.value.node.path.key;
        const relevantPaths = event.nodePaths.filter(nodePath => nodePath.parent.key === currentFolderKey);

        if (relevantPaths.length === 0) {
          return;
        }

        const nextFolder = this._currentFolder$.value.copy();
        relevantPaths.forEach(nodePath => {
          const node = nextFolder.children.find(n => n.path.fileName === nodePath.fileName);
          if (node instanceof FileExplorerFileNode) {
            node.metadata.mediaInfo = null;
          }
        });
        this.updateCurrentFolder(nextFolder);
      });
  }

  public get currentFolderPath$(): Observable<NodePathHeader | null> {
    return this._currentFolderPath$.asObservable();
  }

  public get currentFolder$(): Observable<FileExplorerFolder> {
    return this._currentFolder$.asObservable();
  }

  public changeDirectory(node?: FileExplorerFolderNode | null): void {
    if (this._loadingRepository.status.loading) {
      return;
    }

    firstValueFrom(this.navigateToDirectory(node?.path))
      .then();
  }

  private navigateToDirectory(nodePath?: NodePathHeader | null): Observable<boolean> {
    const query = nodePath
      ? {root: nodePath.rootPath, dir: nodePath.relativePath}
      : {}

    return from(this._router.navigate(
      [PageRoutes.Files],
      {
        queryParams: query
      }));
  }

  private loadDirectoryFromPathName(pathname: string): void {
    if (!pathname.startsWith('/files')) {
      return;
    }

    const queryParams = this._route.snapshot.queryParams;

    const root = 'root' in queryParams
      ? queryParams['root'] as string
      : '';

    const dir = 'dir' in queryParams
      ? queryParams['dir'] as string
      : '';

    this.loadDirectory(root, dir);
  }

  public refreshFolder(): void {
    const path = this._currentFolderPath$.value;
    if (!path) {
      return;
    }

    this._nodeClient.request(path.key,
      client => client.refreshFolder(new RefreshFolderCommand({
        nodePath: this._nodePathConverter.toRequestDto(path)
      })), 'Failed to refresh folder')
      .then(result => result.success(dto => {
        const folder = this._fileExplorerNodeConverter.fromFileExplorerFolder(dto);
        this.updateCurrentFolder(folder);
      }))
  }

  public setFolderSort(sortMode: FileExplorerFolderSortMode, descending: boolean) {
    const path = this._currentFolderPath$.value;
    if (!path) {
      return;
    }

    this._nodeClient.request(path.key,
      client => client.setFolderSortMode(new SetFolderSortCommand({
        nodePath: this._nodePathConverter.toRequestDto(path),
        sortMode: this.toFolderSortMode(sortMode),
        descending: descending
      })), 'Failed to update folder sort')
      .then();
  }

  private async loadDirectory(root: string, relativePath: string): Promise<void> {
    const path = new NodePathHeader(root, relativePath);
    this._currentFolderPath$.next(path);

    const loadingKey = path.key === "" ? "root" : path.key;

    if (this._loadingRepository.isLoading(loadingKey)) {
      return;
    }

    const result = await this._nodeClient.request(loadingKey,
      client => client.getNode(path.rootPath, path.relativePath), 'Error loading directory');

    if (result.result) {
      const folder = this._fileExplorerNodeConverter.fromFileExplorerFolder(result.result);
      this.updateCurrentFolder(folder);
      this._currentFolderPath$.next(folder.node.path);
    }
  }

  private clear(): void {
    this._currentFolderPath$.next(null);
    this._currentFolder$.next(FileExplorerFolder.Default);
  }

  private isCurrentFolder(folderKey: string): boolean {
    return this._currentFolder$.value.node.path.key === folderKey;
  }

  private updateCurrentFolder(folder: FileExplorerFolder): void {
    const device = this._playbackDeviceService.requestPlaybackDevice;
    folder.children.forEach(node => {
      if (node instanceof FileExplorerFileNode) {
        node.updateCanPlay(device);
      }
    });
    this._currentFolder$.next(folder);
  }

  private insertNodeIntoFolder(folder: FileExplorerFolder, node: FileExplorerNode, index: number, eventIndex: number): void {
    if (index === -1) {
      if (eventIndex !== -1 && eventIndex < folder.children.length) {
        folder.children.splice(eventIndex, 0, node);
      }
      else {
        folder.children.push(node);
      }
    }
    else {
      folder.children[index] = node;
    }
  }

  private toFolderSortMode(sortMode: FileExplorerFolderSortMode): FolderSortMode {
    switch (sortMode) {
      case FileExplorerFolderSortMode.Created:
        return FolderSortMode.Created;
      case FileExplorerFolderSortMode.Name:
        return FolderSortMode.Name;
      default:
        return FolderSortMode.Name;
    }
  }
}
