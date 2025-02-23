import { Injectable } from '@angular/core';
import Hls from "hls.js";

@Injectable({
  providedIn: 'root'
})
export class AudioElementRepositoryService {
  private readonly _audio: HTMLAudioElement;
  private readonly _hls: Hls;

  constructor() {
    this._audio = new Audio();
    this._hls = new Hls();
  }

  public get audio(): HTMLAudioElement {
    return this._audio;
  }

  // https://stackoverflow.com/a/64821821/12939184
  // This weirdness is due to iOS safari, not letting you set currentTime until the audio is loaded
  public async playFromTime(currentTime: number, streamUrl: string, transcode: boolean): Promise<void> {
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
    if (transcode && Hls.isSupported()) {
      this._hls.loadSource(streamUrl);
      this._hls.attachMedia(this.audio);
      console.log("HLS attached");
    } else{
      this._hls.detachMedia();
      this.audio.src = streamUrl;
      console.log("HLS detached", streamUrl);
    }
  }

}
