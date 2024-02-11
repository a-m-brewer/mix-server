import {Injectable} from '@angular/core';
import {BehaviorSubject, distinctUntilChanged, filter, firstValueFrom, map, Observable, Subject} from "rxjs";
import {PlaybackSession} from "./models/playback-session";
import {FileExplorerFileNode} from "../../main-content/file-explorer/models/file-explorer-file-node";
import {SessionClient} from "../../generated-clients/mix-server-clients";
import {
  ProblemDetails,
  RequestPlaybackCommand,
  SeekRequest,
  SetCurrentSessionCommand,
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
import {AudioPlayerStateService} from "../audio-player/audio-player-state.service";

@Injectable({
  providedIn: 'root'
})
export class CurrentPlaybackSessionRepositoryService {
  private _loading = new BehaviorSubject<boolean>(false);
  private _pauseRequested$ = new Subject<boolean>();
  private _playbackGranted$ = new Subject<PlaybackGranted>();
  private _currentSession$ = new BehaviorSubject<PlaybackSession | null>(null);

  constructor(audioElementRepository: AudioElementRepositoryService,
              audioPlayerStateService: AudioPlayerStateService,
              private _playbackSessionConverter: PlaybackSessionConverterService,
              private _loadingRepository: LoadingRepositoryService,
              private _sessionClient: SessionClient,
              private _sessionSignalRClient: SessionSignalrClientService,
              private _toastService: ToastService,
              private _authenticationService: AuthenticationService) {
    this._authenticationService.serverConnectionStatus$
      .subscribe(serverConnectionStatus => {
        if (serverConnectionStatus === ServerConnectionState.Connected) {
          const currentSession = this.currentPlaybackSession;
          this._sessionClient.syncPlaybackSession(new SyncPlaybackSessionCommand({
            playbackSessionId: currentSession?.id,
            playing: audioElementRepository.audio.duration > 0 && !audioElementRepository.audio.paused,
            currentTime: audioElementRepository.audio.currentTime
          }))
            .subscribe({
              next: value => {
                if (value.useClientState) {
                  return;
                }

                const nextSession =
                  value.session
                    ? this._playbackSessionConverter.fromDto(value.session)
                    : null;

                this.nextSession(nextSession);
              },
              error: err => {
                if ((err as ProblemDetails)?.status !== 404) {
                  this._toastService.logServerError(err, 'Failed to fetch current session');
                }
              }
            });
        }

        if (serverConnectionStatus === ServerConnectionState.Unauthorized) {
          this.nextSession(null);
        }
      });

    this.initializeSignalR();
  }

  public get currentSession$(): Observable<PlaybackSession | null> {
    return this._currentSession$
      .pipe(distinctUntilChanged((p, n) => p?.id === n?.id))
  }

  public get currentPlaybackDevice$(): Observable<string | null | undefined> {
    return this._currentSession$
      .pipe(distinctUntilChanged((p, n) => p?.state.deviceId === n?.state.deviceId))
      .pipe(map(p => p?.state.deviceId));
  }

  public get currentSessionPlaying$(): Observable<boolean> {
    return this._currentSession$
      .pipe(distinctUntilChanged((p, n) => p?.state.playing === n?.state.playing))
      .pipe(map(m => m?.state.playing ?? false))
  }

  public get loading$(): Observable<boolean> {
    return this._loading.asObservable();
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

  public get currentPlaybackSession(): PlaybackSession | null {
    return this._currentSession$.getValue();
  }

  public setFile(file: FileExplorerFileNode): void  {
    this.loading = true;
    this._loadingRepository.loading = true;

    this._sessionClient.setCurrentSession(new SetCurrentSessionCommand({
      absoluteFolderPath: file.parentFolder.absolutePath ?? '',
      fileName: file.name
    })).subscribe({
      next: _ => {},
      error: err => this._toastService.logServerError(err, 'Failed to set current session')
    });
  }

  public async requestPlayback(deviceId?: string): Promise<void> {
    this._loadingRepository.loading = true;

    const requestedDeviceId = deviceId ?? this._authenticationService.deviceId;

    if (!requestedDeviceId) {
      this._toastService.error('Missing current device id', 'Not Found');
      return;
    }

    await firstValueFrom(this._sessionClient.requestPlayback(new RequestPlaybackCommand({
      deviceId: requestedDeviceId
    }))).catch(err => {
      this._toastService.logServerError(err, 'Failed to request playback');
      this._loadingRepository.loading = false;
    });
  }

  public requestPause(): void {
    this._loadingRepository.loading = true;

    this._sessionClient.requestPause()
      .subscribe({
        error: err => this._toastService.logServerError(err, 'Failed to request pause')
      });
  }

  public clearSession(): void {
    this._sessionClient.clearCurrentSession()
      .subscribe({
        next: _ => this.nextSession(null),
        error: err => this._toastService.logServerError(err, 'Failed to clear current session')
      })
  }

  public back(): void {
    this.setNextSession(new SetNextSessionCommand({
      offset: -1,
      resetSessionState: false
    }))
  }

  public skip(): void {
    this.setNextSession(new SetNextSessionCommand({
      offset: 1,
      resetSessionState: false
    }))
  }

  public setSessionEnded(): void {
    if (!this.currentPlaybackSession || this.currentPlaybackSession.state.deviceId !== this._authenticationService.deviceId) {
      return;
    }

    this.setNextSession(new SetNextSessionCommand({
      offset: 1,
      resetSessionState: true
    }));
  }

  private setNextSession(command: SetNextSessionCommand): void {
    this._sessionClient.setNextSession(command).subscribe({
      next: _ => {},
      error: err => this._toastService.logServerError(err, 'Failed to set next session')
    });
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
          this.nextSession(playbackSession);
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
        next: playbackGranted => {
          this.nextState(playbackGranted);

          if (playbackGranted.deviceId === this._authenticationService.deviceId) {
            this._playbackGranted$.next(playbackGranted);
          }
        }
      })

    this._sessionSignalRClient.pauseRequested$
      .subscribe({
        next: value => {
          this._pauseRequested$.next(value);
        }
      })
  }

  private nextSession(playbackSession: PlaybackSession | null): void {
    this.loading = false;
    this._loadingRepository.loading = false;
    this._currentSession$.next(playbackSession);
  }

  private nextState(state: PlaybackState) {
    const previousSession = this.currentPlaybackSession;
    if (!previousSession) {
      return;
    }

    const session = PlaybackSession.copy(previousSession, state);

    this.nextSession(session);
  }

  private set loading(loading: boolean) {
    if (loading === this._loading.getValue()){
      return;
    }

    this._loading.next(loading);
  }
}
