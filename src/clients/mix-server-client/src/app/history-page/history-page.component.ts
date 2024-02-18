import {Component, OnDestroy, OnInit} from '@angular/core';
import {Subject, takeUntil} from "rxjs";
import {PlaybackSession} from "../services/repositories/models/playback-session";
import {FileExplorerFileNode} from "../main-content/file-explorer/models/file-explorer-file-node";
import {
  CurrentPlaybackSessionRepositoryService
} from "../services/repositories/current-playback-session-repository.service";
import {FileExplorerNode} from "../main-content/file-explorer/models/file-explorer-node";
import {HistoryRepositoryService} from "../services/repositories/history-repository.service";

@Component({
  selector: 'app-history-page',
  templateUrl: './history-page.component.html',
  styleUrls: ['./history-page.component.scss']
})
export class HistoryPageComponent implements OnInit, OnDestroy {
  private _unsubscribe$ = new Subject();

  public loading: boolean = false;
  public sessions: PlaybackSession[] = [];
  public lastFetchHadItems: boolean = true;

  public throttle = 300;
  public scrollDistance = 1;
  public scrollUpDistance = 2;
  public selector: string = '#content-scroll-container';

  constructor(private _historyRepository: HistoryRepositoryService,
              private _playbackSessionRepository: CurrentPlaybackSessionRepositoryService) {
  }

  public ngOnInit(): void {
    this._historyRepository.sessions$
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(sessions => {
        this.sessions = sessions;
      });

    this._historyRepository.loading$
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(loading => {
        this.loading = loading;
      });

    this._historyRepository.lastFetchHadItems$
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(lastFetchHadItems => {
        this.lastFetchHadItems = lastFetchHadItems;
      });
  }

  public ngOnDestroy(): void {
    this._unsubscribe$.next(null);
    this._unsubscribe$.complete();
  }

  public onNodeClick(node: FileExplorerNode) {
    if (!(node instanceof FileExplorerFileNode)) {
      return;
    }

    this._playbackSessionRepository.setFile(node);
  }

  public onScrollDown() {
    this._historyRepository.loadMoreItems().then();
  }
}
