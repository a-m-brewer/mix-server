import { Injectable } from '@angular/core';
import {CurrentPlaybackSessionRepositoryService} from "../repositories/current-playback-session-repository.service";
import {AudioElementRepositoryService} from "./audio-element-repository.service";
import {NativeAudioBridgeService} from "./native-audio-bridge.service";
import {ToastService} from "../toasts/toast-service";

@Injectable({
  providedIn: 'root'
})
export class AudioSessionService {
  constructor(private _audioElementRepository: AudioElementRepositoryService,
              private _nativeAudioBridge: NativeAudioBridgeService,
              private _playbackSessionRepository: CurrentPlaybackSessionRepositoryService,
              private _toastService: ToastService) { }

  public createMetadata(): AudioSessionService {
    const title = this._playbackSessionRepository.currentSession?.currentNode?.path.fileName ?? '';

    if (this._audioElementRepository.isNative) {
      this._nativeAudioBridge.setMetadata(title);
      return this;
    }

    this.metadata = new MediaMetadata({ title });

    return this;
  }

  public updatePositionState(): AudioSessionService {
    if (this._audioElementRepository.isNative) {
      this._nativeAudioBridge.setNowPlayingPosition(
        this._nativeAudioBridge.currentTime,
        this._nativeAudioBridge.duration,
        1.0
      );
      return this;
    }

    this.session.setPositionState({
      duration: this.audio.duration,
      playbackRate: this.audio.playbackRate,
      position: this.audio.currentTime
    });

    return this;
  }

  public setPlaying(): AudioSessionService {
    if (this._audioElementRepository.isNative) {
      return this;
    }
    this.state = "playing";
    return this;
  }

  public setPaused(): AudioSessionService {
    if (this._audioElementRepository.isNative) {
      return this;
    }
    this.state = "paused";
    return this;
  }

  public withPlayActionHandler(handler: () => void): AudioSessionService {
    if (this._audioElementRepository.isNative) {
      return this;
    }
    this.session.setActionHandler('play', handler);
    return this;
  }

  public withPauseActionHandler(handler: () => void): AudioSessionService {
    if (this._audioElementRepository.isNative) {
      return this;
    }
    this.session.setActionHandler('pause', handler);
    return this;
  }

  public withNextTrackActionHandler(handler: (() => void) | null): AudioSessionService {
    if (this._audioElementRepository.isNative) {
      this._nativeAudioBridge.setSkipControls(!!handler, !!this._previousTrackHandler);
      this._nextTrackHandler = handler;
      return this;
    }
    this.session.setActionHandler('nexttrack', handler);
    return this;
  }

  public withPreviousTrackActionHandler(handler: (() => void) | null): AudioSessionService {
    if (this._audioElementRepository.isNative) {
      this._nativeAudioBridge.setSkipControls(!!this._nextTrackHandler, !!handler);
      this._previousTrackHandler = handler;
      return this;
    }
    this.session.setActionHandler('previoustrack', handler);
    return this;
  }

  public withSeekTo(): AudioSessionService {
    if (this._audioElementRepository.isNative) {
      return this;
    }

    try {
      this.session.setActionHandler('seekto', (e) => {
        if (!e.seekTime) { return; }
        if (e.fastSeek && ('fastSeek' in this.audio)) {
          this.audio.fastSeek(e.seekTime);
        }
        else {
          this.audio.currentTime = e.seekTime;
          this.updatePositionState();
        }
      })
    } catch (e) {
      console.error('seekto not supported by browser');
      this._toastService.error('Seeking is not supported by browser', 'Seek unsupported');
    }

    return this;
  }

  // Track skip handler references for native skip control state
  private _nextTrackHandler: (() => void) | null = null;
  private _previousTrackHandler: (() => void) | null = null;

  private get state(): "none" | "paused" | "playing" {
    return this.session.playbackState;
  }

  private set state(state: "none" | "paused" | "playing") {
    this.session.playbackState = state;
  }

  private get session(): MediaSession {
    return window.navigator.mediaSession;
  }

  private get metadata(): MediaMetadata | null {
    return this.session.metadata;
  }

  private set metadata(metadata: MediaMetadata | null) {
    this.session.metadata = metadata;
  }

  private get audio(): HTMLAudioElement {
    return this._audioElementRepository.audio;
  }
}
