import { Injectable } from '@angular/core';
import {CurrentPlaybackSessionRepositoryService} from "../repositories/current-playback-session-repository.service";
import {AudioElementRepositoryService} from "./audio-element-repository.service";
import {ToastService} from "../toasts/toast-service";

@Injectable({
  providedIn: 'root'
})
export class AudioSessionService {
  constructor(private _audioElementRepository: AudioElementRepositoryService,
              private _playbackSessionRepository: CurrentPlaybackSessionRepositoryService,
              private _toastService: ToastService) { }

  public createMetadata(): AudioSessionService {
    this.metadata = new MediaMetadata({
      title: this._playbackSessionRepository.currentPlaybackSession?.currentNode?.name ?? ''
    });

    return this;
  }

  public updatePositionState(): AudioSessionService {
    this.session.setPositionState({
      duration: this.audio.duration,
      playbackRate: this.audio.playbackRate,
      position: this.audio.currentTime
    });

    return this;
  }

  public setPlaying(): AudioSessionService {
    this.state = "playing";
    return this;
  }

  public setPaused(): AudioSessionService {
    this.state = "paused";
    return this;
  }

  public withPlayActionHandler(handler: () => void): AudioSessionService {
    this.session.setActionHandler('play', handler);
    return this;
  }

  public withPauseActionHandler(handler: () => void): AudioSessionService {
    this.session.setActionHandler('pause', handler);
    return this;
  }

  public withNextTrackActionHandler(handler: (() => void) | null): AudioSessionService {
    this.session.setActionHandler('nexttrack', handler);
    return this;
  }

  public withPreviousTrackActionHandler(handler: (() => void) | null): AudioSessionService {
    this.session.setActionHandler('previoustrack', handler);
    return this;
  }

  public withSeekTo(): AudioSessionService {
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
