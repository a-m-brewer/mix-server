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
}
