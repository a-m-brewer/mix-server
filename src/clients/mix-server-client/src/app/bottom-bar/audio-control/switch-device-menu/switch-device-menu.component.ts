import {Component, OnDestroy, OnInit} from '@angular/core';
import {MatIconModule} from "@angular/material/icon";
import {MatMenuModule} from "@angular/material/menu";
import {AsyncPipe, NgForOf} from "@angular/common";
import {MatButtonModule} from "@angular/material/button";
import {AudioPlayerService} from "../../../services/audio-player/audio-player.service";
import {Subject, takeUntil} from "rxjs";
import {Device} from "../../../services/repositories/models/device";
import {
  CurrentPlaybackSessionRepositoryService
} from "../../../services/repositories/current-playback-session-repository.service";
import {PlaybackSession} from "../../../services/repositories/models/playback-session";
import {DeviceRepositoryService} from "../../../services/repositories/device-repository.service";

@Component({
  selector: 'app-switch-device-menu',
  standalone: true,
  imports: [
    MatIconModule,
    MatMenuModule,
    NgForOf,
    AsyncPipe,
    MatButtonModule
  ],
  templateUrl: './switch-device-menu.component.html',
  styleUrl: './switch-device-menu.component.scss'
})
export class SwitchDeviceMenuComponent implements OnInit, OnDestroy {
  private _unsubscribe$ = new Subject();

  public currentDevice: Device | null | undefined = null;
  public currentPlaybackDeviceId: string | null | undefined = null;

  public devices: Device[] = [];
  public session: PlaybackSession | null = null;

  constructor(public audioPlayer: AudioPlayerService,
              private _devicesRepository: DeviceRepositoryService,
              private _sessionRepository: CurrentPlaybackSessionRepositoryService) {
  }

  public ngOnInit(): void {
    this._sessionRepository
      .currentSession$
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(session => {
        this.session = session;
      });

    this._sessionRepository
      .currentPlaybackDevice$
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(currentPlaybackDeviceId => {
        this.currentPlaybackDeviceId = currentPlaybackDeviceId;
        console.log(`pb: ${currentPlaybackDeviceId} d: ${this.currentDevice?.id}`);
      });

    this._devicesRepository
      .currentDevice$
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(currentDevice => {
        this.currentDevice = currentDevice;
        console.log(`pb: ${this.currentPlaybackDeviceId} d: ${this.currentDevice?.id}`);
      });

    this._devicesRepository
      .onlineDevices$
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(devices => {
        this.devices = [...devices.filter(f => f.id !== this.session?.deviceId)];
      });
  }

  public ngOnDestroy(): void {
    this._unsubscribe$.next(null);
    this._unsubscribe$.complete();
  }

  public requestPlayback(device: Device): void {
    this._sessionRepository.requestPlayback(device.id).then();
  }
}
