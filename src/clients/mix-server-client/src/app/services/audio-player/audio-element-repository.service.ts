import { Injectable } from '@angular/core';

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
  public async playFromTime(currentTime: number): Promise<void> {
    let that = this;
    that.audio.load();
    that.audio.pause();
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
}
