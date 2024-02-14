import {Injectable} from '@angular/core';
import {NodeClient} from "../../generated-clients/mix-server-clients";
import {BehaviorSubject, filter, firstValueFrom, from, map, Observable, tap} from "rxjs";
import {FileExplorerNode} from "../../main-content/file-explorer/models/file-explorer-node";
import {FileExplorerNodeConverterService} from "../converters/file-explorer-node-converter.service";
import {LoadingRepositoryService} from "./loading-repository.service";
import {FileExplorerFolderNode} from "../../main-content/file-explorer/models/file-explorer-folder-node";
import {FileExplorerNodeState} from "../../main-content/file-explorer/enums/file-explorer-node-state.enum";
import {ActivatedRoute, NavigationEnd, Router} from "@angular/router";
import {FolderNodeResponse, FolderSortMode, SetFolderSortCommand} from "../../generated-clients/mix-server-clients";
import {PageRoutes} from "../../page-routes.enum";
import {ToastService} from "../toasts/toast-service";
import {AudioPlayerStateService} from "../audio-player/audio-player-state.service";
import {FileExplorerFileNode} from "../../main-content/file-explorer/models/file-explorer-file-node";
import {AuthenticationService} from "../auth/authentication.service";
import {FolderSignalrClientService} from "../signalr/folder-signalr-client.service";
import {FileExplorerFolderSortMode} from "../../main-content/file-explorer/enums/file-explorer-folder-sort-mode";
import {ServerConnectionState} from "../auth/enums/ServerConnectionState";

@Injectable({
  providedIn: 'root'
})
export class FileExplorerNodeRepositoryService {
  private _loading = new BehaviorSubject<boolean>(false);
  private _currentLevelNodes$ = new BehaviorSubject<ReadonlyArray<FileExplorerNode>>([]);
  private _currentFolder$ = new BehaviorSubject<FileExplorerFolderNode>(FileExplorerFolderNode.Default);
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
      })

    this._audioPlayerState.state$
      .subscribe(state => {
        const nextCurrentLevelNodes = [...this._currentLevelNodes$.getValue()]
        nextCurrentLevelNodes.forEach(node => {
          if (node instanceof FileExplorerFileNode) {
            node.updateState(state);
          }
        });

        this._currentLevelNodes$.next(nextCurrentLevelNodes);
      });

    this._folderSignalRClient.folderSorted$()
      .subscribe(updatedFolder => {
        const currentFolder = this._currentFolder$.getValue();
        if (currentFolder.absolutePath !== updatedFolder.parent.absolutePath) {
          return;
        }

        updatedFolder.children.forEach(child => {
          if (child instanceof FileExplorerFileNode) {
            child.updateState(this._audioPlayerState.state);
          }
        })

        this._currentLevelNodes$.next([...updatedFolder.children]);
        this.setLoading(false);
        this._loadingRepository.loading = false;
      });

    this._folderSignalRClient.nodeAdded$()
      .subscribe(node => {
        const currentFolder = this._currentFolder$.getValue();
        if (!node.parentDirectory?.absolutePath || currentFolder.absolutePath !== node.parentDirectory.absolutePath) {
          return
        }

        this._currentLevelNodes$.next([...this._currentLevelNodes$.getValue(), node]);
      })

    this._folderSignalRClient.nodeUpdated$()
      .subscribe(event => {
        const node = event.node;
        const currentFolder = this._currentFolder$.getValue();
        if (!node.parentDirectory?.absolutePath || currentFolder.absolutePath !== node.parentDirectory.absolutePath) {
          return
        }

        const currentLevelNodes = [...this._currentLevelNodes$.getValue()];
        const oldPathIndex = currentLevelNodes.findIndex(n => n.absolutePath === event.oldAbsolutePath);
        const index = oldPathIndex === -1
          ? currentLevelNodes.findIndex(n => n.absolutePath === node.absolutePath)
          : oldPathIndex;

        if (index === -1) {
          currentLevelNodes.push(node)
        }
        else {
          currentLevelNodes[index] = node;
        }

        this._currentLevelNodes$.next(currentLevelNodes);
      });

    this._folderSignalRClient.nodeDeleted$()
      .subscribe(event => {
        const currentFolder = this._currentFolder$.getValue();
        if (currentFolder.absolutePath !== event.parent.absolutePath) {
          return;
        }

        const currentLevelNodes = this._currentLevelNodes$.getValue().filter(f => f.absolutePath !== event.absolutePath);
        this._currentLevelNodes$.next(currentLevelNodes);
      })
  }

  public get currentFolder$(): Observable<FileExplorerFolderNode> {
    return this._currentFolder$.asObservable();
  }

  public getCurrentLevelNodes$(): Observable<ReadonlyArray<FileExplorerNode>> {
    return this._currentLevelNodes$
      .asObservable();
  }

  public get loading$(): Observable<boolean> {
    return this._loading.asObservable();
  }

  public changeDirectory(node?: FileExplorerFolderNode): void {
    if (this._loading.getValue()) {
      return;
    }

    if (node) {
      node.state = FileExplorerNodeState.Loading
    }

    firstValueFrom(this.navigateToDirectory(node?.absolutePath))
      .catch(() => {
        if (node) {
          node.state = FileExplorerNodeState.None;
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
        next: (folder: FolderNodeResponse) => {
          const { parent, children } = this._fileExplorerNodeConverter.fromDto(folder);

          this._loadingRepository.loading = false;
          this.setLoading(false);

          children.forEach((child: FileExplorerNode) => {
            if (child instanceof FileExplorerFileNode) {
              child.updateState(this._audioPlayerState.state);
            }
          })

          this._currentFolder$.next(parent);
          this._currentLevelNodes$.next(children);
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
    const currentFolder = this._currentFolder$.getValue().absolutePath;

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
    this._currentLevelNodes$.next([]);
    this._currentFolder$.next(FileExplorerFolderNode.Default);
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
