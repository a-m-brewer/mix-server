import {Injectable} from '@angular/core';
import {BehaviorSubject, combineLatestWith, map, Observable} from "rxjs";
import {FileExplorerFolder} from "../../main-content/file-explorer/models/file-explorer-folder";
import {LoadingRepositoryService} from "../repositories/loading-repository.service";
import {
  FolderSortMode,
  RefreshFolderCommand,
  SetFolderSortCommand
} from "../../generated-clients/mix-server-clients";
import {FileExplorerNodeConverterService} from "../converters/file-explorer-node-converter.service";
import {FolderSignalrClientService} from "../signalr/folder-signalr-client.service";
import {Device} from "../repositories/models/device";
import {FileExplorerFileNode} from "../../main-content/file-explorer/models/file-explorer-file-node";
import {FileExplorerNode} from "../../main-content/file-explorer/models/file-explorer-node";
import {FileExplorerFolderNode} from "../../main-content/file-explorer/models/file-explorer-folder-node";
import {FileExplorerFolderSortMode} from "../../main-content/file-explorer/enums/file-explorer-folder-sort-mode";
import {PlaybackDeviceRepositoryService} from "../repositories/playback-device-repository.service";
import {MediaInfoUpdatedEventItem} from "../signalr/models/media-info-event";
import {NodeApiService} from "../api.service";
import {NodePath, NodePathHeader} from "../../main-content/file-explorer/models/node-path";
import {NodePathConverterService} from "../converters/node-path-converter.service";
import {AuthenticationService} from "../auth/authentication.service";
import {ServerConnectionState} from "../auth/enums/ServerConnectionState";
import {PagedFileExplorerFolder} from "../../main-content/file-explorer/models/paged-file-explorer-folder";

@Injectable({
  providedIn: 'root'
})
export class NodeCacheService {
  private readonly _pageSize = 25;


  private _folders$ = new BehaviorSubject<{ [nodeKey: string]: PagedFileExplorerFolder }>({});
  private _consumerFolders = new Map<string, Set<string>>();
  private _folderConsumers = new Map<string, Set<string>>();

  private _folderScanInProgress$ = new BehaviorSubject(false);

  constructor(private _authenticationService: AuthenticationService,
              private _fileExplorerNodeConverter: FileExplorerNodeConverterService,
              private _folderSignalRClient: FolderSignalrClientService,
              private _loadingRepository: LoadingRepositoryService,
              private _nodeClient: NodeApiService,
              private _nodePathConverter: NodePathConverterService,
              private _playbackDeviceService: PlaybackDeviceRepositoryService) {
    this._authenticationService.serverConnectionStatus$
      .subscribe(serverConnectionStatus => {
        const loggedIn = serverConnectionStatus === ServerConnectionState.Connected;
        if (loggedIn) {
          this._nodeClient.request("GetScanInProgress", client => client.getFolderScanStatus(), 'Error loading folder scan status')
            .then(dto => {
              if (dto.result) {
                this._folderScanInProgress$.next(dto.result.scanInProgress);
              }
            })
        }
      });

    this._playbackDeviceService.requestPlaybackDevice$
      .subscribe(device => {
        const folders: { [key: string]: PagedFileExplorerFolder } = {};
        Object.entries(this._folders$.value)
          .forEach(([key, value]) => {
            folders[key] = value.copy();
          });
        this.updateFolders(folders, false, device);
      });

    this._folderSignalRClient.folderRefreshed$()
      .subscribe(updatedFolder => {
        this.updateFolder(updatedFolder, true);
      });

    this._folderSignalRClient.folderSorted$()
      .subscribe(updatedFolder => {
        this.updateFolder(updatedFolder, true);
        this._loadingRepository.stopLoading(updatedFolder.node.path.key);
      });

    this._folderSignalRClient.mediaInfoUpdated$()
      .subscribe(event => {
        const groupedUpdates = event.mediaInfo
          .reduce((acc, item) => {
            if (!(item.nodePath.parent.key in acc)) {
              acc[item.nodePath.parent.key] = [];
            }

            acc[item.nodePath.parent.key].push(item);

            return acc;
          }, {} as { [absolutePath: string]: MediaInfoUpdatedEventItem[] });

        const updatedFolders: { [absolutePath: string]: PagedFileExplorerFolder } = {};

        Object.entries(groupedUpdates).forEach(([absolutePath, mediaInfos]) => {
          const parent = this._folders$.value[absolutePath];
          if (!parent) {
            return;
          }

          const nextFolder = parent.copy();
          mediaInfos.forEach(f => {
            const node = nextFolder.flatChildren.find(n => n.path.fileName === f.nodePath.fileName);
            if (node instanceof FileExplorerFileNode) {
              node.metadata.mediaInfo = f.info;
            }
          });

          updatedFolders[absolutePath] = nextFolder;
        });

        this.updateFolders(updatedFolders, false);
      });

    this._folderSignalRClient.mediaInfoRemoved$()
      .subscribe(event => {
        const groupedUpdates = event.nodePaths
          .reduce((acc, item) => {
            if (!(item.parent.key in acc)) {
              acc[item.parent.key] = [];
            }

            acc[item.parent.key].push(item);

            return acc;
          }, {} as { [absolutePath: string]: NodePath[] });

        const updatedFolders: { [absolutePath: string]: PagedFileExplorerFolder } = {};

        Object.entries(groupedUpdates).forEach(([absolutePath, mediaInfos]) => {
          const parent = this._folders$.value[absolutePath];
          if (!parent) {
            return;
          }

          const nextFolder = parent.copy();
          mediaInfos.forEach(f => {
            const node = nextFolder.flatChildren.find(n => n.path.fileName === f.fileName);
            if (node instanceof FileExplorerFileNode) {
              node.metadata.mediaInfo = null;
            }
          });

          updatedFolders[absolutePath] = nextFolder;
        });

        this.updateFolders(updatedFolders, false);
      });

    this._folderSignalRClient.folderScanStatusChanged$()
      .subscribe(scanInProgress => {
        this._folderScanInProgress$.next(scanInProgress);
      });
  }

  public getFolder$(query$: Observable<NodePathHeader>): Observable<PagedFileExplorerFolder> {
    return this._folders$
      .pipe(combineLatestWith(query$))
      .pipe(map(([folders, header]) => folders[header.key] ?? FileExplorerFolder.Default));
  }

  getFileByNode$(initialNode: FileExplorerFileNode): Observable<FileExplorerFileNode> {
    // const existingFolder = this._folders$.value[initialNode.parent.path.key];

    // const nodeIndex = !!existingFolder
    //   ? existingFolder.children.findIndex(n => n.path.isEqual(initialNode.path))
    //   : -1;
    //
    // if (existingFolder && nodeIndex !== -1) {
    //   const nextFolder = existingFolder.copy();
    //   nextFolder.children.splice(nodeIndex, 1, initialNode);
    // }

    return this._folders$
      .pipe(map(folders => {
        const folder = folders[initialNode.parent.path.key];
        const node = folder?.flatChildren.find(n => n.path.isEqual(initialNode.path));

        if (!node || !(node instanceof FileExplorerFileNode)) {
          return initialNode;
        }

        return node;
      }));
  }

  public get folderScanInProgress$(): Observable<boolean> {
    return this._folderScanInProgress$.asObservable();
  }

  public async loadDirectoriesForConsumer(consumerId: string, requiredNodes: NodePathHeader[]): Promise<NodePathHeader[]> {
    const prev = this._consumerFolders.get(consumerId) ?? new Set<string>();

    const loadedNodes: NodePathHeader[] = [];

    for (const nodeHeader of requiredNodes) {
      const consumers = this._folderConsumers.get(nodeHeader.key) ?? new Set();
      consumers.add(consumerId);
      this._folderConsumers.set(nodeHeader.key, consumers);

      if (this._folders$.value[nodeHeader.key]) {
        loadedNodes.push(nodeHeader);
      } else {
        loadedNodes.push(await this.loadDirectoryByKey(nodeHeader));
      }
    }

    const next = new Set(requiredNodes.map(n => n.key));

    for (const key of prev) {
      if (!next.has(key)) {
        const consumers = this._folderConsumers.get(key)!;
        consumers.delete(consumerId);
        if (consumers.size === 0) {
          const nf = { ...this._folders$.value };
          delete nf[key];
          this._folders$.next(nf);
          this._folderConsumers.delete(key);
        } else {
          this._folderConsumers.set(key, consumers);
        }
      }
    }

    this._consumerFolders.set(consumerId, next);

    return loadedNodes;
  }

  private async loadDirectoryByKey(path: NodePathHeader): Promise<NodePathHeader> {
    const loadingKey = path.key === "" ? "root" : path.key;

    if (this._loadingRepository.isLoading(loadingKey)) {
      return path;
    }

    const result = await this._nodeClient.request(loadingKey,
      client => client.getNode(0, this._pageSize, path.rootPath, path.relativePath), 'Error loading directory');

    if (!result.result) {
      return new NodePathHeader("", "");
    }

    const folder = this._fileExplorerNodeConverter.fromPagedFileExplorerFolder(result.result);
    this.updateFolder(folder, true);

    return folder.node.path;
  }

  public refreshFolder(nodePath: NodePathHeader, recursive?: boolean): void {
    this._nodeClient.request(nodePath.key,
      client => client.refreshFolder(new RefreshFolderCommand({
        nodePath: this._nodePathConverter.toRequestDto(nodePath),
        recursive: recursive ?? false,
        pageSize: this._pageSize
      })), 'Failed to refresh folder')
      .then(result => result.success(dto => {
        const folder = this._fileExplorerNodeConverter.fromPagedFileExplorerFolder(dto);
        this.updateFolder(folder, true);
      }))
  }

  public async setFolderSort(nodePath: NodePathHeader, sortMode: FileExplorerFolderSortMode, descending: boolean): Promise<void> {
    await this._nodeClient.request(nodePath.key,
      client => client.setFolderSortMode(new SetFolderSortCommand({
        nodePath: this._nodePathConverter.toRequestDto(nodePath),
        sortMode: this.toFolderSortMode(sortMode),
        descending: descending,
        pageSize: this._pageSize
      })), 'Failed to update folder sort');
  }

  private copyOrCreateParentFromNode(node: FileExplorerNode) {
    return this.copyOrCreateParent(node.parent);
  }

  private copyOrCreateParent(parent: FileExplorerFolderNode | null | undefined): PagedFileExplorerFolder {
    const existingFolder = this._folders$.value[parent?.path.key ?? ""]
    return existingFolder
      ? existingFolder.copy()
      : PagedFileExplorerFolder.Default;
  }

  private updateFolder(folder: PagedFileExplorerFolder, reset: boolean): void {
    this.updateFolders({[folder.node.path.key]: folder}, reset);
  }

  private updateFolders(updates: { [absolutePath: string]: PagedFileExplorerFolder }, reset: boolean, device?: Device | null): void {
    const requestedPlaybackDevice = device ?? this._playbackDeviceService.requestPlaybackDevice;

    const existingFolders = this._folders$.value;

    Object.values(updates).forEach(updatedFolder => {
      // If we're not resetting loaded folders e.g. when sorting, we need to ensure that we keep existing pages.
      if (!reset) {
        const existingFolder = existingFolders[updatedFolder.node.path.key];
        const existingPages = (existingFolder?.pages ?? []);

        existingPages.forEach(page => {
          // The update contains the page, which means its more recent than the existing one.
          if (updatedFolder.pages.some(p => p.pageIndex === page.pageIndex)) {
            return;
          }

          // If the page is not in the update, we need to add it.
          updatedFolder.pages.push(page);
        })
      }

      updatedFolder.flatChildren.forEach(node => {
        if (node instanceof FileExplorerFileNode) {
          node.updateCanPlay(requestedPlaybackDevice);
        }
      })
    });

    const nextFolders = {...existingFolders, ...updates};
    this._folders$.next(nextFolders);
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
