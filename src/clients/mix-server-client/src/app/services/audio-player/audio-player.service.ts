import {Injectable} from '@angular/core';
import {CurrentPlaybackSessionRepositoryService} from "../repositories/current-playback-session-repository.service";
import {
  BehaviorSubject,
  combineLatestWith,
  filter, firstValueFrom,
  map,
  Observable,
  sampleTime
} from "rxjs";
import {StreamUrlService} from "../converters/stream-url.service";
import {AudioSessionService} from "./audio-session.service";
import {AudioElementRepositoryService} from "./audio-element-repository.service";
import {PlaybackSession} from "../repositories/models/playback-session";
import {ToastService} from "../toasts/toast-service";
import {QueueRepositoryService} from "../repositories/queue-repository.service";
import {FileExplorerFileNode} from "../../main-content/file-explorer/models/file-explorer-file-node";
import {AudioPlayerStateService} from "./audio-player-state.service";
import {AuthenticationService} from "../auth/authentication.service";
import {PlaybackGranted} from "../repositories/models/playback-granted";
import {LoadingRepositoryService} from "../repositories/loading-repository.service";
import {DeviceRepositoryService} from "../repositories/device-repository.service";
import {Device} from "../repositories/models/device";
import {SessionService} from "../sessions/session.service";

@Injectable({
  providedIn: 'root'
})
export class AudioPlayerService {
  private _timeChangedBehaviourSubject$ = new BehaviorSubject<number>(0);

  private _playbackGranted: boolean = false;

  private _previousFile: FileExplorerFileNode | null | undefined;
  private _nextFile: FileExplorerFileNode | null | undefined;

  constructor(private _audioElementRepository: AudioElementRepositoryService,
              private _audioSession: AudioSessionService,
              private _audioPlayerState: AudioPlayerStateService,
              private _authenticationService: AuthenticationService,
              private _deviceRepository: DeviceRepositoryService,
              private _loadingRepository: LoadingRepositoryService,
              private _playbackSessionRepository: CurrentPlaybackSessionRepositoryService,
              private _queueRepository: QueueRepositoryService,
              private _sessionService: SessionService,
              private _streamUrlService: StreamUrlService,
              private _toastService: ToastService) {
    this.audio.ontimeupdate = () => {
      this.updateTimeChangedBehaviourSubject();
    }

    this.audio.onended = () => {
      this.handleOnSessionEnded();
    }

    this._playbackSessionRepository
      .currentSession$
      .subscribe(session => {
        if (session) {
          this.setCurrentSession(session);
        } else {
          this.clearSession();
        }
      });

    this._queueRepository
      .queuePosition$()
      .subscribe(item => {
        this._audioPlayerState.queueItemId = item?.id;
      });

    this._queueRepository
      .nextQueueItem$()
      .subscribe(item => {
        this._nextFile = item?.file;
      });

    this._queueRepository
      .previousQueueItem$()
      .subscribe(item => {
        this._previousFile = item?.file;
      })

    this._playbackSessionRepository
      .currentState$
      .subscribe(state => {
        this.currentTime = state.currentTime;
      });

    this._playbackSessionRepository
      .pauseRequested$
      .subscribe(_ => {
        this.handlePauseRequested();
      });

    this._playbackSessionRepository
      .playbackGranted$
      .subscribe(playbackGranted => {
        this.handlePlaybackGranted(playbackGranted);
      });

    this.sampleCurrentTime$(600)
      .subscribe(currentTime => {
        this.updatePlaybackState(currentTime);
      });
  }

  public get currentTime$(): Observable<number> {
    return this._timeChangedBehaviourSubject$.asObservable();
  }

  public get currentTimePercentage$(): Observable<number> {
    return this._timeChangedBehaviourSubject$
      .pipe(map(currentTime => {
        if (this.duration === 0 || currentTime === 0) {
          return 0;
        }

        return (currentTime / this.duration) * 100;
      }));
  }

  public get currentPlaybackDevice$(): Observable<Device | null | undefined> {
    return this._playbackSessionRepository.currentPlaybackDevice$
      .pipe(combineLatestWith(this._deviceRepository.devices$))
      .pipe(map(([currentPlaybackDeviceId, devices]) => {
        return devices.find(d => d.id === currentPlaybackDeviceId);
      }));
  }

  public get otherValidPlaybackDevices$(): Observable<Array<Device>> {
    return this._deviceRepository.onlineDevices$
      .pipe(combineLatestWith(this._playbackSessionRepository.currentPlaybackDevice$))
      .pipe(map(([devices, currentPlaybackDeviceId]) => {
        if (!currentPlaybackDeviceId) {
          return devices.filter(f => f.id !== this._authenticationService.deviceId);
        }

        return devices.filter(d => d.id !== currentPlaybackDeviceId);
      }));
  }

  public get audioControlsDisabled$(): Observable<boolean> {
    return this.currentPlaybackDevice$
      .pipe(combineLatestWith(this._authenticationService.connected$))
      .pipe(map(([device, connected]) => {
        return !connected ||
          (!!device && !device.interactedWith);
      }));
  }

  public get playbackDisabled$(): Observable<boolean> {
    return this.audioControlsDisabled$
      .pipe(combineLatestWith(this._playbackSessionRepository.currentSession$))
      .pipe(map(([disabled, session]) => {
        return disabled || !session || session.currentNode.playbackDisabled;
      }));
  }

  public get previousItemDisabled$(): Observable<boolean> {
    return this.audioControlsDisabled$
      .pipe(combineLatestWith(this._queueRepository.previousQueueItem$()))
      .pipe(map(([disabled, previousItem]) => {
        return disabled || !previousItem || previousItem.disabled;
      }));
  }

  public get nextItemDisabled$(): Observable<boolean> {
    return this.audioControlsDisabled$
      .pipe(combineLatestWith(this._queueRepository.nextQueueItem$()))
      .pipe(map(([disabled, nextItem]) => {
        return disabled || !nextItem || nextItem.disabled;
      }));
  }

  public get isCurrentPlaybackDevice$(): Observable<boolean> {
    return this._playbackSessionRepository.currentPlaybackDevice$
      .pipe(combineLatestWith(this._deviceRepository.currentDevice$))
      .pipe(map(([currentPlaybackDeviceId, device]) => {
        return !!currentPlaybackDeviceId && !!device && currentPlaybackDeviceId === device.id;
      }));
  }

  public get playing$(): Observable<boolean> {
    return this.isCurrentPlaybackDevice$
      .pipe(combineLatestWith(this._playbackSessionRepository.currentSessionPlaying$))
      .pipe(map(([isCurrentPlaybackDevice, currentSessionPlaying]) => {
        return isCurrentPlaybackDevice
          ? this.playing
          : currentSessionPlaying;
      }));
  }

  public sampleCurrentTime$(ms: number, onlyPlaying = true): Observable<number> {
    const behaviour = onlyPlaying
      ? this._timeChangedBehaviourSubject$.pipe(filter((_, __) => this.playing))
      : this._timeChangedBehaviourSubject$;

    return behaviour.pipe(sampleTime(ms));
  }

  public get playing(): boolean {
    return this._playbackGranted && this.audio.duration > 0 && !this.audio.paused
  }

  public get currentTime(): number {
    return this._timeChangedBehaviourSubject$.value;
  }

  public set currentTime(value: number) {
    this._timeChangedBehaviourSubject$.next(value);
    this.audio.currentTime = value;
  }

  public get duration(): number {
    return this.audio.duration;
  }

  public get volume(): number {
    return this.audio.volume;
  }

  public set volume(value: number) {
    this.audio.volume = value;
  }

  public get muted(): boolean {
    return this.audio.muted;
  }

  public set muted(value: boolean) {
    this.audio.muted = value;
  }

  public async requestPlaybackOnCurrentPlaybackDevice(): Promise<void> {
    await this._playbackSessionRepository.requestPlaybackOnCurrentPlaybackDevice();
  }

  public async requestPlayback(deviceId?: string): Promise<void> {
    await this._playbackSessionRepository.requestPlayback(deviceId);
  }

  public requestPause(): void {
    this._playbackGranted = false;

    firstValueFrom(this.isCurrentPlaybackDevice$)
      .then(isCurrentPlaybackDevice => {
        if (isCurrentPlaybackDevice) {
          this.handlePauseRequested();
        } else {
          this._playbackSessionRepository.requestPause();
        }
      });
  }

  public seek(time: number): void {
    const sanitizedTime = Math.min(Math.max(0, time), this.duration);

    this._playbackSessionRepository.seek(sanitizedTime);
  }

  public seekOffset(offset: number): void {
    this.seek(this.currentTime + offset);
  }

  private async play(): Promise<void> {
    try {
      await this._audioElementRepository.playFromTime(this.currentTime);
      this.setDevicePlaying(true);
      this._playbackGranted = true;
      this._audioSession
        .createMetadata()
        .updatePositionState()
        .withPlayActionHandler(() => {
          this.requestPlayback().then();
        })
        .withPauseActionHandler(() => {
          this.requestPause();
        })
        .withSeekTo();

      if (this._nextFile) {
        this._audioSession
          .withNextTrackActionHandler(() => {
            if (!this._nextFile) {
              return
            }

            this._sessionService.skip();
          })
      } else {
        this._audioSession
          .withNextTrackActionHandler(null);
      }

      if (this._previousFile) {
        this._audioSession
          .withPreviousTrackActionHandler(() => {
            if (!this._previousFile) {
              return
            }

            this._sessionService.back();
          })
      } else {
        this._audioSession
          .withPreviousTrackActionHandler(null);
      }

      this.setPlaying();
    } catch (err) {
      if (err instanceof DOMException) {
        if (err.name === 'NotSupportedError') {
          this._toastService.error(`${this._playbackSessionRepository.currentSession?.currentNode?.name} unsupported`, 'Not Supported');
          this._sessionService.clearSession();
        } else {
          console.error(err);
          this._toastService.error(`${this._playbackSessionRepository.currentSession?.currentNode?.name}: ${err.message}`, err.name);
        }
      } else if (err instanceof Error) {
        console.error(err);
        this._toastService.error(`${this._playbackSessionRepository.currentSession?.currentNode?.name}: ${err.message}`, err.name);
      }
    }
  }

  private internalPause(): void {
    this.audio.pause();
    this.setPaused();
  }

  private get audio(): HTMLAudioElement {
    return this._audioElementRepository.audio;
  }

  private setCurrentSession(session: PlaybackSession): void {
    this.audio.src = this._streamUrlService.getStreamUrl(session.id);

    this._timeChangedBehaviourSubject$.next(session.state.currentTime);

    this._audioPlayerState.node = session.currentNode;

    if (session.deviceId === this._authenticationService.deviceId) {
      this.play().then();
    }
  }

  private setPlaying(): void {
    this._audioSession.setPlaying();
    this._audioPlayerState.playing = true;
  }

  private setPaused(): void {
    this._audioSession.setPaused();
    this._audioPlayerState.playing = false;
  }

  private clearSession(): void {
    this.audio.src = '';
    this.audio.load();
    this.updateTimeChangedBehaviourSubject();
    this._audioPlayerState.clear();
  }

  private updatePlaybackState(currentTime: number): void {
    if (!this.playing) {
      return;
    }

    this._playbackSessionRepository.updatePlaybackState(currentTime);
  }

  private handleOnSessionEnded(): void {
    this._sessionService.setSessionEnded();
  }

  private updateTimeChangedBehaviourSubject() {
    this._timeChangedBehaviourSubject$.next(this.audio.currentTime);
  }

  private handlePauseRequested(): void {
    this.internalPause();

    this._playbackSessionRepository.setDevicePlaying(this.currentTime, false);
    this._loadingRepository.stopLoading();
  }

  private handlePlaybackGranted(playbackGranted: PlaybackGranted): void {
    if (!playbackGranted.useDeviceCurrentTime) {
      this.currentTime = playbackGranted.currentTime;
    }

    this.play().then();
  }

  private setDevicePlaying(playing: boolean) {
    this._playbackSessionRepository.setDevicePlaying(this.currentTime, playing);
  }
}
