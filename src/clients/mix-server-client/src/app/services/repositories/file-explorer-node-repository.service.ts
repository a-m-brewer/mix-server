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

@Injectable({
  providedIn: 'root'
})
export class FileExplorerNodeRepositoryService {
  private _currentFolderAbsolutePath$ = new BehaviorSubject<string>("");

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
    return this._nodeCache.getFolder$(this._currentFolderAbsolutePath$);
  }

  public changeDirectory(node?: FileExplorerFolderNode | null): void {
    if (this._loadingRepository.status.loading) {
      return;
    }

    firstValueFrom(this.navigateToDirectory(node?.absolutePath))
      .then();
  }

  private navigateToDirectory(absolutePath?: string | null): Observable<boolean> {
    const query = absolutePath
      ? {dir: absolutePath}
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

    const dir = 'dir' in queryParams
      ? queryParams['dir'] as string
      : '';

    this.loadDirectory(dir);
  }

  public refreshFolder(): void {
    this._nodeCache.refreshFolder(this._currentFolderAbsolutePath$.value).then();
  }

  public setFolderSort(sortMode: FileExplorerFolderSortMode, descending: boolean) {
    if (this._currentFolderAbsolutePath$.value === "") {
      return;
    }

    this._nodeCache.setFolderSort(this._currentFolderAbsolutePath$.value, sortMode, descending)
      .then();
  }

  private loadDirectory(absolutePath: string): void {
    this._nodeCache.loadDirectory(absolutePath)
      .then(loadedPath => {
        this._currentFolderAbsolutePath$.next(loadedPath);
      })
  }

  private clear(): void {
    this._currentFolderAbsolutePath$.next("");
  }
}
