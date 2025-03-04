import { Injectable } from '@angular/core';
import {DeviceRepositoryService} from "../repositories/device-repository.service";
import {CurrentPlaybackSessionRepositoryService} from "../repositories/current-playback-session-repository.service";
import {combineLatestWith, distinctUntilChanged, map, Observable, pipe} from "rxjs";
import {Device} from "../repositories/models/device";
import {AuthenticationService} from "../auth/authentication.service";
import {PlaybackDeviceRepositoryService} from "../repositories/playback-device-repository.service";

@Injectable({
  providedIn: 'root'
})
export class PlaybackDeviceService {
  constructor(private _authenticationService: AuthenticationService,
              private _deviceRepository: DeviceRepositoryService,
              private _playbackDeviceRepository: PlaybackDeviceRepositoryService,
              private _playbackSessionRepository: CurrentPlaybackSessionRepositoryService) {
    this.currentPlaybackDevice$
      .pipe(combineLatestWith(this._deviceRepository.currentDevice$))
      .pipe(map(([currentPlaybackDevice, currentDevice]) => {
        return currentPlaybackDevice ?? currentDevice;
      }))
      .subscribe(device => {
        _playbackDeviceRepository.requestPlaybackDevice = device;
      })
  }

  public get requestPlaybackDevice(): Device | null | undefined {
    return this._playbackDeviceRepository.requestPlaybackDevice;
  }

  public get requestPlaybackDevice$(): Observable<Device | null | undefined> {
    return this._playbackDeviceRepository.requestPlaybackDevice$;
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
}
