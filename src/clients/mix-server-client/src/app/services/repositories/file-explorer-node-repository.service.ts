import {Injectable} from '@angular/core';
import {FileExplorerFolderResponse, NodeClient} from "../../generated-clients/mix-server-clients";
import {BehaviorSubject, filter, firstValueFrom, from, map, Observable, tap} from "rxjs";
import {FileExplorerNodeConverterService} from "../converters/file-explorer-node-converter.service";
import {LoadingRepositoryService} from "./loading-repository.service";
import {FileExplorerFolderNode} from "../../main-content/file-explorer/models/file-explorer-folder-node";
import {FileExplorerNodeState} from "../../main-content/file-explorer/enums/file-explorer-node-state.enum";
import {ActivatedRoute, NavigationEnd, Router} from "@angular/router";
import {FolderSortMode, SetFolderSortCommand} from "../../generated-clients/mix-server-clients";
import {PageRoutes} from "../../page-routes.enum";
import {ToastService} from "../toasts/toast-service";
import {AudioPlayerStateService} from "../audio-player/audio-player-state.service";
import {AuthenticationService} from "../auth/authentication.service";
import {FolderSignalrClientService} from "../signalr/folder-signalr-client.service";
import {FileExplorerFolderSortMode} from "../../main-content/file-explorer/enums/file-explorer-folder-sort-mode";
import {ServerConnectionState} from "../auth/enums/ServerConnectionState";
import {FileExplorerFolder} from "../../main-content/file-explorer/models/file-explorer-folder";
import {FileExplorerNode} from "../../main-content/file-explorer/models/file-explorer-node";

@Injectable({
  providedIn: 'root'
})
export class FileExplorerNodeRepositoryService {
  private _loading = new BehaviorSubject<boolean>(false);
  private _currentFolder$ = new BehaviorSubject<FileExplorerFolder>(FileExplorerFolder.Default);
  private _loggedIn: boolean = false;

  constructor(private _authenticationService: AuthenticationService,
              private _audioPlayerState: AudioPlayerStateService,
              private _client: NodeClient,
              private _fileExplorerNodeConverter: FileExplorerNodeConverterService,
              private _folderSignalRClient: FolderSignalrClientService,
              private _loadingRepository: LoadingRepositoryService,
              private _route: ActivatedRoute,
              private _router: Router,
              private _toastService: ToastService) {
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
      .pipe(map(m => m.urlAfterRedirects))
      .subscribe(_ => {
        if (this._loggedIn) {
          this.loadDirectoryFromPathName(window.location.pathname);
        }
      });

    this._folderSignalRClient.folderSorted$()
      .subscribe(updatedFolder => {
        const currentFolder = this._currentFolder$.getValue();
        if (currentFolder.node.absolutePath !== updatedFolder.node.absolutePath) {
          return;
        }

        this._currentFolder$.next(updatedFolder);
        this.setLoading(false);
        this._loadingRepository.loading = false;
      });

    this._folderSignalRClient.nodeAdded$()
      .subscribe(node => {
        const currentFolder = this._currentFolder$.getValue();
        if (!node.parent?.absolutePath || currentFolder.node.absolutePath !== node.parent.absolutePath) {
          return
        }

        const nextFolder = currentFolder.copy();
        nextFolder.children.push(node);

        this._currentFolder$.next(nextFolder);
      })

    this._folderSignalRClient.nodeUpdated$()
      .subscribe(event => {
        const node = event.node;
        const currentFolder = this._currentFolder$.getValue();
        if (!node.parent?.absolutePath || currentFolder.node.absolutePath !== node.parent.absolutePath) {
          return
        }

        const nextFolder = currentFolder.copy();

        const oldPathIndex = nextFolder.children.findIndex(n => n.absolutePath === event.oldAbsolutePath);
        const index = oldPathIndex === -1
          ? nextFolder.children.findIndex(n => n.absolutePath === node.absolutePath)
          : oldPathIndex;

        if (index === -1) {
          nextFolder.children.push(node)
        }
        else {
          nextFolder.children[index] = node;
        }

        this._currentFolder$.next(nextFolder);
      });

    this._folderSignalRClient.nodeDeleted$()
      .subscribe(event => {
        const currentFolder = this._currentFolder$.getValue();
        if (currentFolder.node.absolutePath !== event.parent.absolutePath) {
          return;
        }

        const nextFolder = currentFolder.copy();
        nextFolder.children = currentFolder.children.filter(f => f.absolutePath !== event.absolutePath);

        this._currentFolder$.next(nextFolder);
      })
  }

  public get currentFolder$(): Observable<FileExplorerFolder> {
    return this._currentFolder$.asObservable();
  }

  public getCurrentLevelNodes$(): Observable<ReadonlyArray<FileExplorerNode>> {
    return this._currentFolder$
      .pipe(map(folder => folder.children));
  }

  public get loading$(): Observable<boolean> {
    return this._loading.asObservable();
  }

  public changeDirectory(node?: FileExplorerFolderNode): void {
    if (this._loading.getValue()) {
      return;
    }

    if (node) {
      node.state.folderState = FileExplorerNodeState.Loading
    }

    firstValueFrom(this.navigateToDirectory(node?.absolutePath))
      .finally(() => {
        if (node) {
          node.state.folderState = FileExplorerNodeState.None;
        }
      });
  }

  private navigateToDirectory(absolutePath?: string | null): Observable<boolean> {
    const query = absolutePath
      ? {dir: absolutePath}
      : {}

    // this._loadingRepository.loading = true;
    this.setLoading(true);

    return from(this._router.navigate(
      [PageRoutes.Files],
      {
        queryParams: query
      }))
      .pipe(tap(() => {
        // this._loadingRepository.loading = false;
      }));
  }

  private loadDirectory(absolutePath?: string | null): void {
    this._loadingRepository.loading = true;
    this.setLoading(true);

    this._client.getNode(absolutePath)
      .subscribe({
        next: (folderResponse: FileExplorerFolderResponse) => {
          const folder = this._fileExplorerNodeConverter.fromFileExplorerFolder(folderResponse);

          this._loadingRepository.loading = false;
          this.setLoading(false);

          this._currentFolder$.next(folder);
        },
        error: err => {
          this._toastService.logServerError(err, `Failed to navigate to directory ${absolutePath}`);
          this._loadingRepository.loading = false;
          this.navigateToDirectory(null);
        }
      });
  }

  private loadDirectoryFromPathName(pathname: string): void {
    if (!pathname.startsWith('/files')) {
      return;
    }

    const queryParams = this._route.snapshot.queryParams;

    const dir = 'dir' in queryParams
      ? queryParams['dir'] as string
      : null;

    this.loadDirectory(dir);
  }

  public setFolderSort(sortMode: FileExplorerFolderSortMode, descending: boolean) {
    const currentFolder = this._currentFolder$.getValue().node.absolutePath;

    if (!currentFolder) {
      return;
    }

    this.setLoading(true);
    this._loadingRepository.loading = true;

    this._client.setFolderSortMode(new SetFolderSortCommand({
      absoluteFolderPath: currentFolder,
      sortMode: this.toFolderSortMode(sortMode),
      descending: descending
    })).subscribe({
      error: err => this._toastService.logServerError(err, 'Failed to update folder sort')
    })
  }

  private clear(): void {
    this._currentFolder$.next(FileExplorerFolder.Default);
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

  private setLoading(loading: boolean) {
    if (loading === this._loading.getValue()) {
      return;
    }

    this._loading.next(loading);
  }
}
