import {Injectable} from '@angular/core';
import {Title} from "@angular/platform-browser";
import {FileExplorerNodeRepositoryService} from "../repositories/file-explorer-node-repository.service";
import {AudioPlayerStateService} from "../audio-player/audio-player-state.service";
import {NavigationEnd, Router} from "@angular/router";
import {filter, map} from "rxjs";
import {PageRoutes} from "../../page-routes.enum";

@Injectable({
  providedIn: 'root'
})
export class TitleService {

  private _app: string = 'Mix Server';
  private _currentFolderName?: string | null;
  private _currentPlayingFile?: string | null;

  constructor(private _audioStateRepository: AudioPlayerStateService,
              private _nodeRepository: FileExplorerNodeRepositoryService,
              private _router: Router,
              private _title: Title) {
    _title.setTitle(this._app);
  }

  public initialize(): void {
    this._router.events
      .pipe(filter((e): e is NavigationEnd => e instanceof NavigationEnd))
      .pipe(map(m => {
        const url = window.location.pathname;
        return url.startsWith('/') ? url.substring(1) : url;
      }))
      .subscribe(_ => {
        this.setTitle();
      })

    this._nodeRepository.currentFolder$
      .subscribe(folder => {
        this._currentFolderName = folder.node.name;
        this.setTitle();
      });

    this._audioStateRepository.state$
      .subscribe(state => {
        this._currentPlayingFile = state?.playing && state.node?.name
          ? state.node.name
          : null;

        this.setTitle();
      })
  }

  private get page(): string | null {
    const value = window.location.pathname.replace('/', '');

    const keyIndex = Object.values(PageRoutes).findIndex(f => f.toString() === value);

    if (keyIndex === -1) {
      return null;
    }

    return Object.keys(PageRoutes)[keyIndex];
  }

  private setTitle(): void {

    let titleParts: string[] = [];

    if (this._currentPlayingFile) {
      titleParts.push(this._currentPlayingFile);
    }

    if (this._currentFolderName) {
      titleParts.push(this._currentFolderName);
    }

    const page = this.page;
    if (page) {
      titleParts.push(page);
    }

    titleParts.push(this._app);

    const title = titleParts.join(' - ');

    this._title.setTitle(title);
  }
}
