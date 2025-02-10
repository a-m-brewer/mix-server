import { Injectable } from '@angular/core';
import Hls from "hls.js";

@Injectable({
  providedIn: 'root'
})
export class AudioElementRepositoryService {
  private readonly _audio: HTMLAudioElement;

  constructor() {
    this._audio = new Audio();
  }

  public get audio(): HTMLAudioElement {
    return this._audio;
  }

  // https://stackoverflow.com/a/64821821/12939184
  // This weirdness is due to iOS safari, not letting you set currentTime until the audio is loaded
  public async playFromTime(currentTime: number, streamUrl: string, transcode: boolean): Promise<void> {
    this.audio.load();
    this.audio.pause();

    this.attachHls(transcode, streamUrl);

    this.audio.currentTime = currentTime;

    let loadedMetadata = () => {
      this.audio.currentTime = currentTime;
      this.audio.removeEventListener("loadedmetadata", loadedMetadata);
    }

    if(this.audio.currentTime !== currentTime){
      this.audio.addEventListener("loadedmetadata", loadedMetadata);
    }

    await this.audio.play();
  }

  public attachHls(transcode: boolean, streamUrl: string) {
    if (transcode && Hls.isSupported()) {
      const hls = new Hls();
      hls.loadSource(streamUrl);
      hls.attachMedia(this.audio);
    } else{
      this.audio.src = streamUrl
    }
  }
}
