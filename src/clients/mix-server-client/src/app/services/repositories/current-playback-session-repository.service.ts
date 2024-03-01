import {Injectable} from '@angular/core';
import {BehaviorSubject, distinctUntilChanged, filter, firstValueFrom, map, Observable, Subject} from "rxjs";
import {PlaybackSession} from "./models/playback-session";
import {
  ProblemDetails,
  RequestPlaybackCommand,
  SeekRequest,
  SessionClient,
  SetNextSessionCommand,
  SetPlayingCommand,
  SyncPlaybackSessionCommand
} from "../../generated-clients/mix-server-clients";
import {SessionSignalrClientService} from "../signalr/session-signalr-client.service";
import {PlaybackSessionConverterService} from "../converters/playback-session-converter.service";
import {LoadingRepositoryService} from "./loading-repository.service";
import {ToastService} from "../toasts/toast-service";
import {AuthenticationService} from "../auth/authentication.service";
import {PlaybackState} from "./models/playback-state";
import {PlaybackGranted} from "./models/playback-granted";
import {ServerConnectionState} from "../auth/enums/ServerConnectionState";
import {AudioElementRepositoryService} from "../audio-player/audio-element-repository.service";

@Injectable({
  providedIn: 'root'
})
export class CurrentPlaybackSessionRepositoryService {

  private _pauseRequested$ = new Subject<boolean>();
  private _playbackGranted$ = new Subject<PlaybackGranted>();
  private _currentSession$ = new BehaviorSubject<PlaybackSession | null>(null);

  constructor(audioElementRepository: AudioElementRepositoryService,
              private _playbackSessionConverter: PlaybackSessionConverterService,
              private _loadingRepository: LoadingRepositoryService,
              private _sessionClient: SessionClient,
              private _sessionSignalRClient: SessionSignalrClientService,
              private _toastService: ToastService,
              private _authenticationService: AuthenticationService) {
    this._authenticationService.serverConnectionStatus$
      .subscribe(serverConnectionStatus => {
        if (serverConnectionStatus === ServerConnectionState.Connected) {
          const currentSession = this.currentSession;
          this._loadingRepository.startLoading();
          firstValueFrom(this._sessionClient.syncPlaybackSession(new SyncPlaybackSessionCommand({
            playbackSessionId: currentSession?.id,
            playing: audioElementRepository.audio.duration > 0 && !audioElementRepository.audio.paused,
            currentTime: audioElementRepository.audio.currentTime
          })))
            .then(value => {
              if (value.useClientState) {
                return;
              }

              this.currentSession = value.session
                ? this._playbackSessionConverter.fromDto(value.session)
                : null;
            })
            .catch(err => {
              if ((err as ProblemDetails)?.status !== 404) {
                this._toastService.logServerError(err, 'Failed to fetch current session');
              }
            })
            .finally(() => this._loadingRepository.stopLoading());
        }

        if (serverConnectionStatus === ServerConnectionState.Unauthorized) {
          this.currentSession = null;
        }
      });

    this.initializeSignalR();
  }

  public get currentSession(): PlaybackSession | null {
    return this._currentSession$.getValue();
  }

  public set currentSession(value: PlaybackSession | null) {
    this._currentSession$.next(value);
  }

  public get currentSession$(): Observable<PlaybackSession | null> {
    return this._currentSession$
      .pipe(distinctUntilChanged((p, n) => p?.id === n?.id))
  }

  public get currentPlaybackDevice$(): Observable<string | null | undefined> {
    return this._currentSession$
      // .pipe(distinctUntilChanged((p, n) => p?.state.deviceId === n?.state.deviceId))
      .pipe(map(p => p?.state.deviceId));
  }

  public get currentSessionPlaying$(): Observable<boolean> {
    return this._currentSession$
      .pipe(distinctUntilChanged((p, n) => p?.state.playing === n?.state.playing))
      .pipe(map(m => m?.state.playing ?? false))
  }

  public get currentState$(): Observable<PlaybackState> {
    return this._currentSession$
      .pipe(filter<PlaybackSession | null>(Boolean))
      .pipe(map(s => s.state));
  }

  public get pauseRequested$(): Observable<boolean> {
    return this._pauseRequested$.asObservable();
  }

  public get playbackGranted$(): Observable<PlaybackGranted> {
    return this._playbackGranted$.asObservable();
  }

  public async requestPlaybackOnCurrentPlaybackDevice(): Promise<void> {
    return this.requestPlayback(this._currentSession$.value?.state.deviceId)
  }

  public async requestPlayback(deviceId?: string | null): Promise<void> {
    this._loadingRepository.startLoadingId(deviceId)

    const requestedDeviceId = deviceId ?? this._authenticationService.deviceId;

    if (!requestedDeviceId) {
      this._toastService.error('Missing current device id', 'Not Found');
      this._loadingRepository.stopLoadingId(deviceId);
      return;
    }

    await firstValueFrom(this._sessionClient.requestPlayback(new RequestPlaybackCommand({
      deviceId: requestedDeviceId
    })))
      .then(dto => {
        const playbackGranted = this._playbackSessionConverter.fromPlaybackGrantedDto(dto);
        this.handlePlaybackGranted(playbackGranted);
      })
      .catch(err => this._toastService.logServerError(err, 'Failed to request playback'))
      .finally(() => this._loadingRepository.stopLoadingId(deviceId));
  }

  public requestPause(): void {
    this._loadingRepository.startLoading();

    firstValueFrom(this._sessionClient.requestPause())
      .catch(err => this._toastService.logServerError(err, 'Failed to request pause'))
      .finally(() => this._loadingRepository.stopLoading());
  }

  public updatePlaybackState(currentTime: number): void {
    const value = this._currentSession$.getValue();
    if (!value) {
      return;
    }

    this._sessionSignalRClient.updatePlaybackState(currentTime);
  }

  public setDevicePlaying(currentTime: number, playing: boolean) {
    this._sessionClient.setPlaying(new SetPlayingCommand({
      playing,
      currentTime
    })).subscribe({
      error: err => this._toastService.logServerError(err, 'Failed to set session to paused')
    });
  }

  public seek(time: number) {
    this._sessionClient.seek(new SeekRequest({
      time
    })).subscribe({
      error: err => this._toastService.logServerError(err, 'Failed to seek')
    })
  }

  private initializeSignalR(): void {
    this._sessionSignalRClient.currentPlaybackSessionUpdated$()
      .subscribe({
        next: playbackSession => {
          this.currentSession = playbackSession;
        }
      });

    this._sessionSignalRClient.playbackState$
      .subscribe({
        next: state => {
          this.nextState(state);
        }
      });

    this._sessionSignalRClient.playbackGranted$
      .subscribe({
        next: playbackGranted => this.handlePlaybackGranted(playbackGranted)
      })

    this._sessionSignalRClient.pauseRequested$
      .subscribe({
        next: value => {
          this._pauseRequested$.next(value);
        }
      })
  }

  private nextState(state: PlaybackState) {
    const previousSession = this.currentSession;
    if (!previousSession) {
      return;
    }

    this.currentSession = PlaybackSession.copy(previousSession, state);
  }

  private handlePlaybackGranted(playbackGranted: PlaybackGranted): void {
    this.nextState(playbackGranted);

    if (playbackGranted.deviceId === this._authenticationService.deviceId) {
      this._playbackGranted$.next(playbackGranted);
    }
  }
}
