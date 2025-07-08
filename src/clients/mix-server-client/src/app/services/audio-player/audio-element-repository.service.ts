import { Injectable } from '@angular/core';
import Hls from "hls.js";
import {Subject} from "rxjs";

@Injectable({
  providedIn: 'root'
})
export class AudioElementRepositoryService {
  private _onTimeUpdate$ = new Subject<void>();
  private _onEnded$ = new Subject<void>();
  private _onDurationChange$ = new Subject<void>();

  private readonly _audio: HTMLAudioElement;
  private readonly _hls: Hls;

  constructor() {
    this._audio = new Audio();

    this._audio.ontimeupdate = () => {
      this._onTimeUpdate$.next();
    }

    this._audio.onended = () => {
      this._onEnded$.next();
    }

    this._audio.ondurationchange = () => {
      this._onDurationChange$.next();
    }

    this._hls = new Hls();
  }

  public get onTimeUpdate$() {
    return this._onTimeUpdate$.asObservable();
  }

  public get onEnded$() {
    return this._onEnded$.asObservable();
  }

  public get onDurationChange$() {
    return this._onDurationChange$.asObservable();
  }

  public get currentTime(): number {
    return this._audio.currentTime;
  }

  public set currentTime(value: number) {
    this._audio.currentTime = value;
  }

  public get duration(): number {
    return this._audio.duration;
  }

  public get sanitizedDuration(): number {
    return !!this._audio && !isNaN(this._audio.duration) ? this._audio.duration : 0
  }

  public get src(): string {
    return this._audio.src;
  }

  public set src(value: string) {
    this._audio.src = value;
  }

  public get playbackRate(): number {
    return this._audio.playbackRate;
  }

  public get paused(): boolean {
    return this._audio.paused;
  }

  public get muted(): boolean {
    return this._audio.muted;
  }

  public set muted(value: boolean) {
    this._audio.muted = value;
  }

  public get volume(): number {
    return this._audio.volume;
  }

  public set volume(value: number) {
    this._audio.volume = value;
  }

  public load(): void {
    this._audio.load();
  }

  public pause(): void {
    this._audio.pause();
  }

  public play(): Promise<void> {
    return this._audio.play();
  }

  public canPlayType(mimeType: string): CanPlayTypeResult {
    return this._audio.canPlayType(mimeType);
  }

  public seek(time: number, fastSeek?: boolean): void {
    if (fastSeek && ('fastSeek' in this._audio)) {
      this._audio.fastSeek(time);
    }
    else {
      this.currentTime = time;
    }
  }

  // https://stackoverflow.com/a/64821821/12939184
  // This weirdness is due to iOS safari, not letting you set currentTime until the audio is loaded
  public async playFromTime(currentTime: number, streamUrl: string, transcode: boolean): Promise<void> {
    let that = this;
    that.load();
    that.pause();

    this.attachHls(streamUrl, transcode);

    that.currentTime = currentTime;

    let loadedMetadata: () => void;
    loadedMetadata = function() {
      that.currentTime = currentTime;
      that._audio.removeEventListener("loadedmetadata", loadedMetadata);
    }
    if(that.currentTime !== currentTime){
      that._audio.addEventListener("loadedmetadata", loadedMetadata);
    }

    await that.play();
  }

  public attachHls(streamUrl: string, transcode: boolean) {
    if (transcode && Hls.isSupported()) {
      this._hls.loadSource(streamUrl);
      this._hls.attachMedia(this._audio);
    } else{
      this._hls.detachMedia();
      this.src = streamUrl;
    }
  }

}
