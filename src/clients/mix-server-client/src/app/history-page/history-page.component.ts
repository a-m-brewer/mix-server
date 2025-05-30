import {Component, OnDestroy, OnInit} from '@angular/core';
import {Subject, takeUntil} from "rxjs";
import {PlaybackSession} from "../services/repositories/models/playback-session";
import {HistoryRepositoryService} from "../services/repositories/history-repository.service";
import {LoadingNodeStatus, LoadingNodeStatusImpl} from "../services/repositories/models/loading-node-status";
import {LoadingRepositoryService} from "../services/repositories/loading-repository.service";
import {
  NodeListItemChangedEvent
} from "../components/nodes/node-list/node-list-item/interfaces/node-list-item-changed-event";
import {AudioPlayerStateService} from "../services/audio-player/audio-player-state.service";
import {AudioPlayerStateModel} from "../services/audio-player/models/audio-player-state-model";
import {SessionService} from "../services/sessions/session.service";
import {AuthenticationService} from "../services/auth/authentication.service";

@Component({
  selector: 'app-history-page',
  templateUrl: './history-page.component.html',
  styleUrls: ['./history-page.component.scss']
})
export class HistoryPageComponent implements OnInit, OnDestroy {
  private _unsubscribe$ = new Subject();

  public audioPlayerState: AudioPlayerStateModel = new AudioPlayerStateModel();
  public loadingStatus: LoadingNodeStatus = LoadingNodeStatusImpl.new;
  public sessions: PlaybackSession[] = [];
  public moreItemsAvailable: boolean = true;

  public throttle = 300;
  public scrollDistance = 1;
  public scrollUpDistance = 2;
  public selector: string = '#content-scroll-container';

  constructor(private _authenticationService: AuthenticationService,
              private _audioPlayerStateService: AudioPlayerStateService,
              private _historyRepository: HistoryRepositoryService,
              private _loadingRepository: LoadingRepositoryService,
              private _sessionService: SessionService) {
  }

  public ngOnInit(): void {
    this._authenticationService.connected$
      .subscribe(connected => {
        if (connected) {
          this._historyRepository.loadMoreItems().then();
        }
      });

    this._audioPlayerStateService.state$
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(state => {
        this.audioPlayerState = state;
      });

    this._historyRepository.sessions$
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(sessions => {
        this.sessions = sessions;
      });

    this._historyRepository.moreItemsAvailable$
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(moreItemsAvailable => {
        this.moreItemsAvailable = moreItemsAvailable;
      });

    this._loadingRepository.status$()
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(status => {
        this.loadingStatus = status;
      });
  }

  public ngOnDestroy(): void {
    this._unsubscribe$.next(null);
    this._unsubscribe$.complete();
  }

  public onNodeClick(event: NodeListItemChangedEvent) {
    const session = this.sessions.find(f => f.currentNode.path.key === event.key)

    if (!session) {
      return;
    }

    this._sessionService.setFile(session.currentNode);
  }

  public onScrollDown() {
    this._historyRepository.loadMoreItems().then();
  }
}
