import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';
import { Capacitor } from '@capacitor/core';
import { NativeAudio } from '../../plugins/native-audio';

@Injectable({
  providedIn: 'root'
})
export class NativeAudioBridgeService {
  private _currentTime = 0;
  private _duration = 0;
  private _paused = true;

  public readonly onTimeUpdate$ = new Subject<number>();
  public readonly onDurationChange$ = new Subject<number>();
  public readonly onEnded$ = new Subject<void>();
  public readonly onPlayRequest$ = new Subject<void>();
  public readonly onPauseRequest$ = new Subject<void>();
  public readonly onSeekRequest$ = new Subject<number>();
  public readonly onNextTrackRequest$ = new Subject<void>();
  public readonly onPreviousTrackRequest$ = new Subject<void>();
  public readonly onError$ = new Subject<string>();

  constructor() {
    if (Capacitor.isNativePlatform()) {
      this.initialize();
    }
  }

  private initialize(): void {
    NativeAudio.addListener('timeUpdate', (data) => {
      this._currentTime = data.currentTime;
      this.onTimeUpdate$.next(data.currentTime);
    });

    NativeAudio.addListener('durationChange', (data) => {
      this._duration = data.duration;
      this.onDurationChange$.next(data.duration);
    });

    NativeAudio.addListener('ended', () => {
      this._paused = true;
      this.onEnded$.next();
    });

    NativeAudio.addListener('playRequest', () => this.onPlayRequest$.next());
    NativeAudio.addListener('pauseRequest', () => this.onPauseRequest$.next());
    NativeAudio.addListener('seekRequest', (data) => this.onSeekRequest$.next(data.time));
    NativeAudio.addListener('nextTrackRequest', () => this.onNextTrackRequest$.next());
    NativeAudio.addListener('previousTrackRequest', () => this.onPreviousTrackRequest$.next());
    NativeAudio.addListener('error', (data) => this.onError$.next(data.message));
  }

  public get currentTime(): number {
    return this._currentTime;
  }

  public set currentTime(value: number) {
    this._currentTime = value;
    NativeAudio.seek({ time: value });
  }

  public get duration(): number {
    return this._duration;
  }

  public get paused(): boolean {
    return this._paused;
  }

  public async setSource(url: string): Promise<void> {
    await NativeAudio.setSource({ url });
  }

  public async play(fromTime?: number): Promise<void> {
    this._paused = false;
    await NativeAudio.play({ fromTime });
  }

  public pause(): void {
    this._paused = true;
    NativeAudio.pause();
  }

  public stop(): void {
    this._paused = true;
    this._currentTime = 0;
    this._duration = 0;
    NativeAudio.stop();
  }

  public setMetadata(title: string, duration?: number): void {
    NativeAudio.setMetadata({ title, duration });
  }

  public setNowPlayingPosition(position: number, duration: number, playbackRate: number): void {
    NativeAudio.setNowPlayingPosition({ position, duration, playbackRate });
  }

  public setSkipControls(hasNext: boolean, hasPrevious: boolean): void {
    NativeAudio.setSkipControls({ hasNext, hasPrevious });
  }
}
