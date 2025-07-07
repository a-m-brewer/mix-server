import {Injectable} from '@angular/core';
import {BehaviorSubject, filter, firstValueFrom, from, map, Observable, tap} from "rxjs";
import {LoadingRepositoryService} from "./loading-repository.service";
import {FileExplorerFolderNode} from "../../main-content/file-explorer/models/file-explorer-folder-node";
import {ActivatedRoute, NavigationEnd, Router} from "@angular/router";
import {PageRoutes} from "../../page-routes.enum";
import {AuthenticationService} from "../auth/authentication.service";
import {FileExplorerFolderSortMode} from "../../main-content/file-explorer/enums/file-explorer-folder-sort-mode";
import {ServerConnectionState} from "../auth/enums/ServerConnectionState";
import {FileExplorerFolder} from "../../main-content/file-explorer/models/file-explorer-folder";
import {NodeCacheService} from "../nodes/node-cache.service";
import {NodePathHeader} from "../../main-content/file-explorer/models/node-path";

@Injectable({
  providedIn: 'root'
})
export class FileExplorerNodeRepositoryService {
  private _currentFolderPath$ = new BehaviorSubject<NodePathHeader>(NodePathHeader.Default);

  private _loggedIn: boolean = false;

  constructor(private _authenticationService: AuthenticationService,
              private _loadingRepository: LoadingRepositoryService,
              private _nodeCache: NodeCacheService,
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
      .pipe(map(m => m.urlAfterRedirects))
      .subscribe(_ => {
        if (this._loggedIn) {
          this.loadDirectoryFromPathName(window.location.pathname);
        }
      });
  }

  public get currentFolder$(): Observable<FileExplorerFolder> {
    return this._nodeCache.getFolder$(this._currentFolderPath$);
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
    this._nodeCache.refreshFolder(this._currentFolderPath$.value)
  }

  public setFolderSort(sortMode: FileExplorerFolderSortMode, descending: boolean) {
    if (this._currentFolderPath$.value.empty) {
      return;
    }

    this._nodeCache.setFolderSort(this._currentFolderPath$.value, sortMode, descending)
      .then();
  }

  private loadDirectory(root: string, relativePath: string): void {
    this._nodeCache.loadDirectoriesForConsumer("file-explorer", [new NodePathHeader(root, relativePath)])
      .then(loadedPath => {
        this._currentFolderPath$.next(loadedPath[0]);
      })
  }

  private clear(): void {
    this._currentFolderPath$.next(NodePathHeader.Default);
  }
}
