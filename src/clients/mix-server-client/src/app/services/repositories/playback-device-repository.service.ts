import { Injectable } from '@angular/core';
import {BehaviorSubject, Observable} from "rxjs";
import {Device} from "./models/device";

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
    this._requestPlaybackDevice$.next(device);
  }

  public get requestPlaybackDevice$(): Observable<Device | null | undefined> {
    return this._requestPlaybackDevice$.asObservable();
  }
}
