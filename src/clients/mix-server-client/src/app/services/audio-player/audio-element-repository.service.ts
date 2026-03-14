import { Injectable } from '@angular/core';
import Hls from "hls.js";
import { Capacitor } from '@capacitor/core';
import { NativeAudioBridgeService } from './native-audio-bridge.service';

@Injectable({
  providedIn: 'root'
})
export class AudioElementRepositoryService {
  private readonly _audio: HTMLAudioElement;
  private readonly _hls: Hls;
  private readonly _isNative: boolean;

  constructor(private _nativeAudioBridge: NativeAudioBridgeService) {
    this._audio = new Audio();
    this._hls = new Hls();
    this._isNative = Capacitor.isNativePlatform();
  }

  public get isNative(): boolean {
    return this._isNative;
  }

  public get audio(): HTMLAudioElement {
    return this._audio;
  }

  // https://stackoverflow.com/a/64821821/12939184
  // This weirdness is due to iOS safari, not letting you set currentTime until the audio is loaded
  public async playFromTime(currentTime: number, streamUrl: string, transcode: boolean): Promise<void> {
    if (this._isNative) {
      await this._nativeAudioBridge.setSource(streamUrl);
      await this._nativeAudioBridge.play(currentTime);
      return;
    }

    let that = this;
    that.audio.load();
    that.audio.pause();

    this.attachHls(streamUrl, transcode);

    that.audio.currentTime = currentTime;

    let loadedMetadata: () => void;
    loadedMetadata = function() {
      that.audio.currentTime = currentTime;
      that.audio.removeEventListener("loadedmetadata", loadedMetadata);
    }
    if(that.audio.currentTime !== currentTime){
      that.audio.addEventListener("loadedmetadata", loadedMetadata);
    }

    await that.audio.play();
  }

  public attachHls(streamUrl: string, transcode: boolean) {
    if (this._isNative) {
      this._nativeAudioBridge.setSource(streamUrl);
      return;
    }

    if (transcode && Hls.isSupported()) {
      this._hls.loadSource(streamUrl);
      this._hls.attachMedia(this.audio);
    } else{
      this._hls.detachMedia();
      this.audio.src = streamUrl;
    }
  }

}
