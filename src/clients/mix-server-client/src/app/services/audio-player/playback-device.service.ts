import { Injectable } from '@angular/core';
import {DeviceRepositoryService} from "../repositories/device-repository.service";
import {CurrentPlaybackSessionRepositoryService} from "../repositories/current-playback-session-repository.service";
import {combineLatestWith, distinctUntilChanged, map, Observable} from "rxjs";
import {Device} from "../repositories/models/device";
import {AuthenticationService} from "../auth/authentication.service";

@Injectable({
  providedIn: 'root'
})
export class PlaybackDeviceService {
  public requestPlaybackDevice: Device | null | undefined;

  constructor(private _authenticationService: AuthenticationService,
              private _deviceRepository: DeviceRepositoryService,
              private _playbackSessionRepository: CurrentPlaybackSessionRepositoryService) {
    this.requestPlaybackDevice$.subscribe(requestedDevice => {
      this.requestPlaybackDevice = requestedDevice;
    });
  }

  public get requestPlaybackDevice$(): Observable<Device | null | undefined> {
    return this.currentPlaybackDevice$
      .pipe(combineLatestWith(this._deviceRepository.currentDevice$))
      .pipe(map(([currentPlaybackDevice, currentDevice]) => {
        return currentPlaybackDevice ?? currentDevice;
      }))
      .pipe(distinctUntilChanged((prev, next) => this.devicesMatch(prev, next)));
  }

  public get currentPlaybackDevice$(): Observable<Device | null | undefined> {
    return this._playbackSessionRepository.currentPlaybackDevice$
      .pipe(combineLatestWith(this._deviceRepository.devices$))
      .pipe(map(([currentPlaybackDeviceId, devices]) => {
        if (!currentPlaybackDeviceId) {
          return null;
        }

        return devices.find(d => d.id === currentPlaybackDeviceId);
      }));
  }

  public get otherValidPlaybackDevices$(): Observable<Array<Device>> {
    return this._deviceRepository.onlineDevices$
      .pipe(combineLatestWith(this._playbackSessionRepository.currentPlaybackDevice$, this._playbackSessionRepository.currentSession$))
      .pipe(map(([devices, currentPlaybackDeviceId, session]) => {
        if (!currentPlaybackDeviceId) {
          return devices.filter(d => d.id !== this._authenticationService.deviceId && d.canPlay(session?.currentNode));
        }

        return devices.filter(d => d.id !== currentPlaybackDeviceId && d.canPlay(session?.currentNode));
      }));
  }

  private devicesMatch(prev: Device | null | undefined, next: Device | null | undefined): boolean {
    if (!prev && !next) {
      return true;
    }
    if (!prev || !next) {
      return false;
    }

    return prev.id === next.id && this.sameCapabilities(prev.capabilities, next.capabilities);
  }

  sameCapabilities(prev: { [mimeType: string]: boolean }, next: { [mimeType: string]: boolean }) {
    const prevKeys = Object.keys(prev);
    const nextKeys = Object.keys(next);

    if (prevKeys.length !== nextKeys.length) return false;

    return prevKeys.every(key => key in next && prev[key] === next[key]);
  }
}
