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
import {HistoryDataSource} from "./history-data-source";
import {CurrentPlaybackSessionRepositoryService} from "../services/repositories/current-playback-session-repository.service";

@Component({
    selector: 'app-history-page',
    templateUrl: './history-page.component.html',
    styleUrls: ['./history-page.component.scss'],
    standalone: false
})
export class HistoryPageComponent implements OnInit, OnDestroy {
  private _unsubscribe$ = new Subject();

  public audioPlayerState: AudioPlayerStateModel = new AudioPlayerStateModel();
  public loadingStatus: LoadingNodeStatus = LoadingNodeStatusImpl.new;
  public dataSource!: HistoryDataSource;

  constructor(private _authenticationService: AuthenticationService,
              private _audioPlayerStateService: AudioPlayerStateService,
              private _currentPlaybackSessionRepository: CurrentPlaybackSessionRepositoryService,
              private _historyRepository: HistoryRepositoryService,
              private _loadingRepository: LoadingRepositoryService,
              private _sessionService: SessionService) {
  }

  public ngOnInit(): void {
    // Initialize data source
    this.dataSource = new HistoryDataSource(
      (start, end) => this._historyRepository.fetchRange(start, end),
      () => this._historyRepository.getInitialLength()
    );

    this._authenticationService.connected$
      .subscribe(connected => {
        if (connected) {
          this.dataSource.initialize().then();
        }
      });

    this._audioPlayerStateService.state$
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(state => {
        this.audioPlayerState = state;
      });

    this._loadingRepository.status$()
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(status => {
        this.loadingStatus = status;
      });

    // Handle live session updates
    this._currentPlaybackSessionRepository.currentSession$
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(session => {
        if (session) {
          // When a new session is played, add it to the data source
          this.dataSource.handleNewSession(session);
        }
      });
  }

  public ngOnDestroy(): void {
    this._unsubscribe$.next(null);
    this._unsubscribe$.complete();
    this.dataSource.disconnect();
  }

  public onNodeClick(event: NodeListItemChangedEvent) {
    // Find the session by key in the current data
    const session = this.dataSource.currentData.find(f => f.currentNode.path.key === event.key);

    if (!session) {
      return;
    }

    this._sessionService.setFile(session.currentNode);
  }
}
