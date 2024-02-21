import {Injectable} from '@angular/core';
import {CurrentPlaybackSessionRepositoryService} from "../repositories/current-playback-session-repository.service";
import {BehaviorSubject, filter, Observable, sampleTime, Subject, takeUntil, timer} from "rxjs";
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

@Injectable({
  providedIn: 'root'
})
export class AudioPlayerService {
  private _timeChangedBehaviourSubject$ = new BehaviorSubject<number>(0);

  private _unsubscribe$ = new Subject();

  private _playbackGranted: boolean = false;

  private _previousFile:  FileExplorerFileNode | null | undefined;
  private _nextFile:  FileExplorerFileNode | null | undefined;

  constructor(private _audioElementRepository: AudioElementRepositoryService,
              private _audioSession: AudioSessionService,
              private _audioPlayerState: AudioPlayerStateService,
              private _authenticationService: AuthenticationService,
              private _loadingRepository: LoadingRepositoryService,
              private _playbackSessionRepository: CurrentPlaybackSessionRepositoryService,
              private _queueRepository: QueueRepositoryService,
              private _streamUrlService: StreamUrlService,
              private _toastService: ToastService) {
    this.audio.ontimeupdate = () => {
      this.updateTimeChangedBehaviourSubject();
    }

    this.audio.onended = () => {
      this.handleOnSessionEnded();
    }
  }

  public initialize(): void {
    this._playbackSessionRepository
      .currentSession$
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(session => {
        if (session) {
          this.setCurrentSession(session);
        }
        else {
          this.clearSession();
        }
      });

    this._queueRepository
      .queuePosition$()
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(item => {
        this._audioPlayerState.queueItemId = item?.id;
      });

    this._queueRepository
      .nextQueueItem$()
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(item => {
        this._nextFile = item?.file;
      });

    this._queueRepository
      .previousQueueItem$()
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(item => {
        this._previousFile = item?.file;
      })

    this._playbackSessionRepository
      .currentState$
      .pipe(takeUntil(this._unsubscribe$))
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
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(currentTime => {
        this.updatePlaybackState(currentTime);
      });
  }

  public dispose(): void {
    this._unsubscribe$.next(null);
    this._unsubscribe$.complete();
  }

  public get currentTime$(): Observable<number> {
    return this._timeChangedBehaviourSubject$.asObservable();
  }

  public sampleCurrentTime$(ms: number): Observable<number> {
    return this._timeChangedBehaviourSubject$
      .pipe(filter((_, __) => this.playing))
      .pipe(sampleTime(ms));
  }

  public get playing(): boolean {
    return this._playbackGranted && this.audio.duration > 0 && !this.audio.paused
  }

  public get currentTime(): number {
    return this.audio.currentTime;
  }

  public set currentTime(value: number) {
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

  public async requestPlayback(deviceId?: string): Promise<void> {
    await this._playbackSessionRepository.requestPlayback(deviceId);
  }

  public requestPause(): void {
    this._playbackGranted = false;
    this._playbackSessionRepository.requestPause();
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
      await this.audio.play();
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

            this._playbackSessionRepository.skip();
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

            this._playbackSessionRepository.back();
          })
      } else {
        this._audioSession
          .withPreviousTrackActionHandler(null);
      }

      this.setPlaying();
    } catch (err) {
      const dom = err as DOMException;
      if (dom.name === 'NotSupportedError') {
        this._toastService.error(`${this._playbackSessionRepository.currentPlaybackSession?.currentNode?.name} unsupported`, 'Not Supported');
        this._playbackSessionRepository.clearSession();
      } else {
        console.error(dom);
        this._toastService.error(`${this._playbackSessionRepository.currentPlaybackSession?.currentNode?.name}: ${dom.message}`, dom.name);
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
    this.audio.load();
    this.audio.currentTime = session.state.currentTime;

    this._audioPlayerState.node = session.currentNode;

    if (session.autoPlay && session.deviceId === this._authenticationService.deviceId) {
      this.play().then();
    }
    else {
      this.setPaused();
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
    this._playbackSessionRepository.setSessionEnded();
  }

  private updateTimeChangedBehaviourSubject() {
    this._timeChangedBehaviourSubject$.next(this.currentTime);
  }

  private handlePauseRequested(): void {
    console.log('pause requested');
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
