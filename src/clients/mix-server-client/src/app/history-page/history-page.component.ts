import {AfterViewInit, Component, OnDestroy, OnInit} from '@angular/core';
import {firstValueFrom, Subject} from "rxjs";
import {PlaybackSession} from "../services/repositories/models/playback-session";
import {SessionClient} from "../generated-clients/mix-server-clients";
import {PlaybackSessionConverterService} from "../services/converters/playback-session-converter.service";
import {LoadingRepositoryService} from "../services/repositories/loading-repository.service";
import {FileExplorerFileNode} from "../main-content/file-explorer/models/file-explorer-file-node";
import {FileExplorerNodeRepositoryService} from "../services/repositories/file-explorer-node-repository.service";
import {ToastService} from "../services/toasts/toast-service";
import {GetUsersSessionsResponse} from "../generated-clients/mix-server-clients";
import {AudioPlayerStateService} from "../services/audio-player/audio-player-state.service";
import {
  CurrentPlaybackSessionRepositoryService
} from "../services/repositories/current-playback-session-repository.service";
import {NodeListItem} from "../components/nodes/node-list/node-list-item/models/node-list-item";
import {AuthenticationService} from "../services/auth/authentication.service";

@Component({
  selector: 'app-history-page',
  templateUrl: './history-page.component.html',
  styleUrls: ['./history-page.component.scss']
})
export class HistoryPageComponent implements OnInit, AfterViewInit, OnDestroy {
  private _unsubscribe$ = new Subject();


  public loading: boolean = false;
  public sessions: PlaybackSession[] = [];
  public lastFetchHadItems: boolean = true;

  public throttle = 300;
  public scrollDistance = 1;
  public scrollUpDistance = 2;
  public selector: string = '#content-scroll-container';

  constructor(private _authService: AuthenticationService,
              private _audioPlayerState: AudioPlayerStateService,
              private _loadingRepository: LoadingRepositoryService,
              private _sessionClient: SessionClient,
              private _converter: PlaybackSessionConverterService,
              private _playbackSessionRepository: CurrentPlaybackSessionRepositoryService,
              private _toastService: ToastService) {
  }

  public ngOnInit(): void {
    this._audioPlayerState.state$
      .subscribe(state => {
        this.sessions.forEach(session => {
          session.currentNode.updateState(state);
        })
      })

    this._authService.connected$
      .subscribe(connected => {
        if (connected) {
          this.sessions = [];
          this.loadMoreItems().then();
        }
      })
  }

  public ngAfterViewInit(): void {
  }

  public ngOnDestroy(): void {
    this._unsubscribe$.next(null);
    this._unsubscribe$.complete();
  }

  public onNodeClick(node: NodeListItem) {
    if (!(node instanceof FileExplorerFileNode)) {
      return;
    }

    this._playbackSessionRepository.setFile(node);
  }

  public onScrollDown() {
    this.loadMoreItems().then();
  }

  private async loadMoreItems(): Promise<void> {
    if (this.loading || !this.lastFetchHadItems) {
      return;
    }

    this.loading = true;
    this._loadingRepository.loading = true;

    const history = await firstValueFrom(this._sessionClient.history(this.sessions.length, 15))
      .catch(err => {
        this._toastService.logServerError(err, 'Failed to fetch history');
        return new GetUsersSessionsResponse();
      });

    this.lastFetchHadItems = history.sessions.length > 0;

    if (this.lastFetchHadItems) {
      this.sessions.push(...history.sessions.map(m => this._converter.fromDto(m)));

      this.sessions.forEach(session => {
        session.currentNode.updateState(this._audioPlayerState.state);
      })
    }

    this.loading = false;
    this._loadingRepository.loading = false;
  }
}
