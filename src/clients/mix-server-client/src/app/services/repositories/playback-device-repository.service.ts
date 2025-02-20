import { Injectable } from '@angular/core';
import {BehaviorSubject, Observable} from "rxjs";
import {Device} from "./models/device";
import sameCapabilities from "../audio-player/same-capabilities";

@Injectable({
  providedIn: 'root'
})
export class PlaybackDeviceRepositoryService {
  private _requestPlaybackDevice$ = new BehaviorSubject<Device | null | undefined>(null);

  constructor() { }

  public get requestPlaybackDevice(): Device | null | undefined {
    return this._requestPlaybackDevice$.value;
  }

  public set requestPlaybackDevice(device: Device | null | undefined) {
    if (this.devicesMatch(this._requestPlaybackDevice$.value, device)) {
      return;
    }

    this._requestPlaybackDevice$.next(device);
  }

  public get requestPlaybackDevice$(): Observable<Device | null | undefined> {
    return this._requestPlaybackDevice$.asObservable();
  }

  private devicesMatch(prev: Device | null | undefined, next: Device | null | undefined): boolean {
    if (!prev && !next) {
      return true;
    }
    if (!prev || !next) {
      return false;
    }

    return prev.id === next.id && sameCapabilities(prev.capabilities, next.capabilities);
  }
}
