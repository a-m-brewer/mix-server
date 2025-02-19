import {Injectable} from '@angular/core';
import {CurrentPlaybackSessionRepositoryService} from "../repositories/current-playback-session-repository.service";
import {
  BehaviorSubject, combineLatest,
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
import {LoadingRepositoryService} from "../repositories/loading-repository.service";
import {DeviceRepositoryService} from "../repositories/device-repository.service";
import {SessionService} from "../sessions/session.service";
import {PlaybackGrantedEvent} from "../repositories/models/playback-granted-event";
import {Mutex} from "async-mutex";
import {timespanToTotalSeconds} from "../../utils/timespan-helpers";
import {MediaMetadata} from "../../main-content/file-explorer/models/media-metadata";
import {AudioPlayerCapabilitiesService} from "./audio-player-capabilities.service";
import {PlaybackDeviceService} from "./playback-device.service";

@Injectable({
  providedIn: 'root'
})
export class AudioPlayerService {
  private _playMutex = new Mutex();

  private _timeChangedBehaviourSubject$ = new BehaviorSubject<number>(0);

  private _clientDurationBehaviourSubject$ = new BehaviorSubject<number>(this.getSanitizedClientDuration());
  private _serverDurationBehaviourSubject$ = new BehaviorSubject<number>(0);

  private _streamUrl: string = '';
  private _transcode: boolean = false;
  private _playbackGranted: boolean = false;

  private _previousFile: FileExplorerFileNode | null | undefined;
  private _nextFile: FileExplorerFileNode | null | undefined;

  constructor(private _audioElementRepository: AudioElementRepositoryService,
              private _audioSession: AudioSessionService,
              private _audioPlayerState: AudioPlayerStateService,
              private _authenticationService: AuthenticationService,
              private _deviceRepository: DeviceRepositoryService,
              private _loadingRepository: LoadingRepositoryService,
              private _playbackDeviceService: PlaybackDeviceService,
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

    this.audio.ondurationchange = () => {
      this._clientDurationBehaviourSubject$.next(this.getSanitizedClientDuration());
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
        const delta =  this.currentTime - state.currentTime;
        if (this.isCurrentPlaybackDevice && delta === 0) {
          return;
        }
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

  public get audioControlsDisabled$(): Observable<boolean> {
    return combineLatest(
      [
        this._playbackDeviceService.requestPlaybackDevice$,
        this._authenticationService.connected$,
        this._loadingRepository.status$()
      ])
      .pipe(map(([device, connected, loadingStatus]) => {
        return !connected || (!!device && !(device.interactedWith || device.isCurrentDevice)) || loadingStatus.loading;
      }));
  }

  public get playbackDisabled$(): Observable<boolean> {
    return combineLatest([
      this.audioControlsDisabled$,
      this._playbackSessionRepository.currentSession$,
      this._playbackDeviceService.requestPlaybackDevice$
    ])
      .pipe(map(([disabled, session, device]) => {
        return disabled || !session || !device || !device.canPlay(session.currentNode);
      }));
  }

  public get previousItemDisabled$(): Observable<boolean> {
    return this.audioControlsDisabled$
      .pipe(combineLatestWith(this._queueRepository.queue$()))
      .pipe(map(([disabled, queue]) => {
        return disabled ||
          !queue ||
          !queue.hasValidOffset(-1);
      }));
  }

  public get nextItemDisabled$(): Observable<boolean> {
    return this.audioControlsDisabled$
      .pipe(combineLatestWith(this._queueRepository.queue$()))
      .pipe(map(([disabled, queue]) => {
        return disabled ||
          !queue ||
          !queue.hasValidOffset(1);
      }));
  }

  public get isCurrentPlaybackDevice$(): Observable<boolean> {
    return this._playbackSessionRepository.currentPlaybackDevice$
      .pipe(combineLatestWith(this._deviceRepository.currentDevice$))
      .pipe(map(([currentPlaybackDeviceId, device]) => {
        return !!currentPlaybackDeviceId && !!device && currentPlaybackDeviceId === device.id;
      }));
  }

  private get isCurrentPlaybackDevice(): boolean {
    return this._playbackSessionRepository.currentPlaybackDevice === this._authenticationService.deviceId;
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

  public get currentCueIndex$(): Observable<number> {
    return this.sampleCurrentTime$(500, false)
      .pipe(combineLatestWith(this._playbackSessionRepository.currentSession$))
      .pipe(map(([currentTime, session]) => {
        if (session?.currentNode.metadata instanceof MediaMetadata) {
          for (let i = session.currentNode.metadata.tracklist.controls.cues.controls.length - 1; i >= 0; i--) {
            const cue = session.currentNode.metadata.tracklist.controls.cues.controls[i].value.cue;
            if (!cue) {
              continue;
            }

            const cueStartTimeSeconds = timespanToTotalSeconds(cue);

            if (cueStartTimeSeconds <= currentTime) {
              return i;
            }
          }
        }

        return -1;
      }));
  }

  public get currentCue$() {
    return this.currentCueIndex$
      .pipe(combineLatestWith(this._playbackSessionRepository.currentSession$))
      .pipe(map(([cueIndex, session]) => {
        if (session?.currentNode.metadata instanceof MediaMetadata) {
          return session.currentNode.metadata.tracklist.controls.cues.controls[cueIndex]?.value;
        }

        return null;
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

  public get duration$(): Observable<number> {
    return this._serverDurationBehaviourSubject$
      .pipe(combineLatestWith(this._clientDurationBehaviourSubject$))
      .pipe(map(([serverDuration, clientDuration]) => this.getDuration(serverDuration, clientDuration)));
  }

  public get duration(): number {
    return this.getDuration(this._serverDurationBehaviourSubject$.value, this._clientDurationBehaviourSubject$.value);
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

  public async play(): Promise<void> {
    await this._playMutex.runExclusive(async () => {
      if (this.playing) {
        return;
      }

      await this.playInternal();
    });
  }

  private async playInternal(): Promise<void> {
    try {
      this._playbackGranted = true;
      await this._audioElementRepository.playFromTime(this.currentTime, this._streamUrl, this._transcode);
      this.setDevicePlaying(true);
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
    const serverDuration = session.currentNode.metadata instanceof MediaMetadata
      ? timespanToTotalSeconds(session.currentNode.metadata.duration)
      : 0;
    this._serverDurationBehaviourSubject$.next(serverDuration);

    this._streamUrl = this._streamUrlService.getStreamUrl(session.id);
    this._transcode = !session.currentNode.directPlaybackSupported;

    this._audioElementRepository.attachHls(this._streamUrl, this._transcode);

    this._timeChangedBehaviourSubject$.next(session.state.currentTime);

    this._audioPlayerState.node = session.currentNode;

    if (session.deviceId === this._authenticationService.deviceId) {
      this.play().then();
    }
  }

  private setPlaying(): void {
    this._audioSession.setPlaying();
    this._audioPlayerState.playing = true;
    if (this.isCurrentPlaybackDevice) {
      this._playbackSessionRepository.setDevicePlayingState(true);
    }
    this._loadingRepository.stopLoadingAction('RequestPlayback');
  }

  private setPaused(): void {
    this._audioSession.setPaused();
    this._audioPlayerState.playing = false;
    if (this.isCurrentPlaybackDevice) {
      this._playbackSessionRepository.setDevicePlayingState(false);
    }
  }

  private clearSession(): void {
    this.audio.src = '';
    this.audio.load();
    this.updateTimeChangedBehaviourSubject();
    this._audioPlayerState.clear();
    this._serverDurationBehaviourSubject$.next(0);
    this._clientDurationBehaviourSubject$.next(0);
  }

  private updatePlaybackState(currentTime: number): void {
    if (!this.playing) {
      return;
    }

    this._playbackSessionRepository.updatePlaybackState(currentTime);

    if (this.isCurrentPlaybackDevice) {
      this._playbackSessionRepository.setDevicePlayingStateCurrentTime(currentTime)
    }
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
    this._loadingRepository.stopLoadingAction('PauseRequested');
  }

  private handlePlaybackGranted(playbackGranted: PlaybackGrantedEvent): void {
    if (!playbackGranted.useDeviceCurrentTime) {
      this.currentTime = playbackGranted.currentTime;
    }

    if (playbackGranted.granted) {
      this.play().then();
    }
    else {
      this.internalPause();
      this.currentTime = playbackGranted.currentTime;
    }
  }

  private setDevicePlaying(playing: boolean) {
    this._playbackSessionRepository.setDevicePlaying(this.currentTime, playing);
  }

  private getSanitizedClientDuration(): number {
    return !!this.audio && !isNaN(this.audio.duration) ? this.audio.duration : 0
  }

  private getDuration(serverDuration: number, clientDuration: number): number {
    return Math.max(serverDuration, clientDuration, 0)
  }
}
