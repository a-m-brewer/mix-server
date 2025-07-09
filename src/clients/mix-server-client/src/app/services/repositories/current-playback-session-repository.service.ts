import {Injectable} from '@angular/core';
import {
  BehaviorSubject,
  distinctUntilChanged,
  filter,
  map, merge,
  Observable,
  Subject, Subscription
} from "rxjs";
import {PlaybackSession} from "./models/playback-session";
import {
  ImportTracklistDto,
  RequestPlaybackCommand,
  SeekRequest,
  SetPlayingCommand,
  SyncPlaybackSessionCommand
} from "../../generated-clients/mix-server-clients";
import {SessionSignalrClientService} from "../signalr/session-signalr-client.service";
import {PlaybackSessionConverterService} from "../converters/playback-session-converter.service";
import {LoadingRepositoryService} from "./loading-repository.service";
import {AuthenticationService} from "../auth/authentication.service";
import {PlaybackState} from "./models/playback-state";
import {PlaybackGranted} from "./models/playback-granted";
import {ServerConnectionState} from "../auth/enums/ServerConnectionState";
import {AudioElementRepositoryService} from "../audio-player/audio-element-repository.service";
import {PlaybackGrantedEvent} from "./models/playback-granted-event";
import {TracklistConverterService} from "../converters/tracklist-converter.service";
import {markAllAsDirty} from "../../utils/form-utils";
import {SessionApiService} from "../api.service";
import {FileExplorerFileNode} from "../../main-content/file-explorer/models/file-explorer-file-node";

@Injectable({
  providedIn: 'root'
})
export class CurrentPlaybackSessionRepositoryService {

  private _pauseRequested$ = new Subject<boolean>();
  private _playbackGranted$ = new Subject<PlaybackGrantedEvent>();
  private _tracklistChanged$ = new Subject<void>();
  private _currentSession$ = new BehaviorSubject<PlaybackSession | null>(null);

  private _currentNodeSub: Subscription | null | undefined = null;
  private _currentSessionNode$ = new BehaviorSubject<FileExplorerFileNode | null>(null);

  constructor(audioElementRepository: AudioElementRepositoryService,
              private _playbackSessionConverter: PlaybackSessionConverterService,
              private _loadingRepository: LoadingRepositoryService,
              private _sessionClient: SessionApiService,
              private _sessionSignalRClient: SessionSignalrClientService,
              private _tracklistConverter: TracklistConverterService,
              private _authenticationService: AuthenticationService) {
    this._authenticationService.serverConnectionStatus$
      .subscribe(serverConnectionStatus => {
        if (serverConnectionStatus === ServerConnectionState.Connected) {
          const currentSession = this.currentSession;

          this._sessionClient.request('SyncPlaybackSession',
            client => client.syncPlaybackSession(new SyncPlaybackSessionCommand({
              playbackSessionId: currentSession?.id,
              playing: audioElementRepository.duration > 0 && !audioElementRepository.paused,
              currentTime: audioElementRepository.currentTime
            })), 'Failed to sync playback session', {
              validStatusCodes: [404]
            })
            .then(result => result.success(dto => {
              if (dto.useClientState) {
                return;
              }

              this.currentSession = dto.session
                ? this._playbackSessionConverter.fromDto(dto.session)
                : null;
            }));
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
    if (this._currentSession$.value) {
      this._currentSession$.value.destroy();
    }

    if (this._currentNodeSub) {
      this._currentNodeSub.unsubscribe();
    }

    this._currentNodeSub = value?.currentNode$.subscribe(node => {
      console.log('nodeSub', node);
      this._currentSessionNode$.next(node);
    })

    console.log(value);
    this._currentSession$.next(value);
  }

  public get currentSession$(): Observable<PlaybackSession | null> {
    return this._currentSession$
      .pipe(distinctUntilChanged((p, n) => p?.id === n?.id))
  }

  public get currentSessionTracklistChanged$(): Observable<PlaybackSession | null> {
    return merge(
      this._currentSession$,
      this._currentSessionNode$,
      this._tracklistChanged$
    )
      .pipe(map(() => this.currentSession));
  }

  public get currentPlaybackDevice$(): Observable<string | null | undefined> {
    return this._currentSession$
      // .pipe(distinctUntilChanged((p, n) => p?.state.deviceId === n?.state.deviceId))
      .pipe(map(p => p?.state.deviceId));
  }

  public get currentPlaybackDevice(): string | null | undefined {
    return this.currentSession?.state.deviceId;
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

  public get playbackGranted$(): Observable<PlaybackGrantedEvent> {
    return this._playbackGranted$.asObservable();
  }

  public async requestPlaybackOnCurrentPlaybackDevice(): Promise<void> {
    return this.requestPlayback(this._currentSession$.value?.state.deviceId)
  }

  public async requestPlayback(deviceId?: string | null): Promise<void> {
    const requestDeviceId = deviceId === null ? undefined : deviceId;
    await this._sessionClient.request('RequestPlayback',
      client => client.requestPlayback(new RequestPlaybackCommand({
        deviceId: requestDeviceId
      })), 'Failed to request playback')
      .then(result => result.success(dto => {
        const playbackGranted = this._playbackSessionConverter.fromPlaybackGrantedDto(dto);
        this.handlePlaybackGranted(playbackGranted);
      }));
  }

  public requestPause(): void {
    this._sessionClient.request('RequestPause', client => client.requestPause(), 'Failed to request pause').then();
  }

  public updatePlaybackState(currentTime: number): void {
    const value = this._currentSession$.getValue();
    if (!value) {
      return;
    }

    this._sessionSignalRClient.updatePlaybackState(currentTime);
  }

  public setDevicePlaying(currentTime: number, playing: boolean) {
    this._sessionClient.request('SetPlaying', client => client.setPlaying(new SetPlayingCommand({
      playing,
      currentTime
    })), 'Failed to set playing').then();
  }

  public seek(time: number) {
    this._sessionClient.request('Seek', client => client.seek(new SeekRequest({
      time
    })), 'Failed to seek').then();
  }

  public updateCurrentSessionTracklist(tracklist: ImportTracklistDto, dirty: boolean): void {
    const previousSession = this.currentSession;
    if (!previousSession || !(previousSession.currentNode.metadata.isMedia)) {
      return;
    }

    const form = this._tracklistConverter.createTracklistForm(tracklist);
    console.log(form, tracklist);
    if (dirty) {
      markAllAsDirty(form);
    }

    const nextSession = PlaybackSession.copy(previousSession, previousSession.state);

    if (nextSession) {
      nextSession.tracklist = form;
    }

    this.currentSession = nextSession;
    this._tracklistChanged$.next();
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
      });

    this._sessionSignalRClient.pauseRequested$
      .subscribe({
        next: value => {
          this._pauseRequested$.next(value);
        }
      });

    this._sessionSignalRClient.tracklistUpdated$
      .subscribe({
        next: event => {
          const previousSession = this.currentSession;
          if (!previousSession) {
            return;
          }

          if (!previousSession.currentNode.path.isEqual(event.path)) {
            return;
          }

          const nextSession = PlaybackSession.copy(previousSession, previousSession.state);
          nextSession.tracklist = event.tracklist;
          this.currentSession = nextSession;
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
      this.nextPlaybackGrantedEvent(playbackGranted.useDeviceCurrentTime, true, playbackGranted.currentTime);
    } else {
      this._loadingRepository.stopLoadingAction('RequestPlayback');
    }
  }

  public setDevicePlayingState(playing: boolean) {
    const state = this.currentSession?.state;
    if (!state) {
      return;
    }

    const nextState = state.copy()
    nextState.playing = playing;

    this.nextState(nextState)
  }

  public setDevicePlayingStateCurrentTime(currentTime: number): void {
    const state = this.currentSession?.state;
    if (!state) {
      return;
    }

    const nextState = state.copy()
    nextState.currentTime = currentTime;

    this.nextState(nextState)
  }

  private nextPlaybackGrantedEvent(useDeviceCurrentTime: boolean, granted: boolean, currentTime: number): void {
    this._playbackGranted$.next({
      useDeviceCurrentTime,
      granted,
      currentTime
    });
  }
}
