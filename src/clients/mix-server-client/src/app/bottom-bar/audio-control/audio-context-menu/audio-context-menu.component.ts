import {Component, OnDestroy, OnInit} from '@angular/core';
import {Subject, takeUntil} from "rxjs";
import {
  CurrentPlaybackSessionRepositoryService
} from "../../../services/repositories/current-playback-session-repository.service";
import {PlaybackSession} from "../../../services/repositories/models/playback-session";
import {DeviceRepositoryService} from "../../../services/repositories/device-repository.service";
import {Device} from "../../../services/repositories/models/device";
import {AuthenticationService} from "../../../services/auth/authentication.service";
import {Router} from "@angular/router";
import {PageRoutes} from "../../../page-routes.enum";

@Component({
  selector: 'app-audio-context-menu',
  templateUrl: './audio-context-menu.component.html',
  styleUrls: ['./audio-context-menu.component.scss']
})
export class AudioContextMenuComponent implements OnInit, OnDestroy{
  private _unsubscribe$ = new Subject();

  public devices: Device[] = [];
  public session: PlaybackSession | null = null;
  public disconnected: boolean = true;

  constructor(
    private _authService: AuthenticationService,
    private _devicesRepository: DeviceRepositoryService,
    private _router: Router,
    private _sessionRepository: CurrentPlaybackSessionRepositoryService) {
  }

  public ngOnInit(): void {
    this._authService.connected$
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(connected => this.disconnected = !connected);

    this._sessionRepository
      .currentSession$
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(session => {
        this.session = session;
      });

    this._devicesRepository
      .onlineDevices$
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(devices => {
        this.devices = [...devices.filter(f => f.id !== this.session?.deviceId)];
      })
  }

  public ngOnDestroy(): void {
    this._unsubscribe$.next(null);
    this._unsubscribe$.complete();
  }

  public clearSession(): void {
    this._sessionRepository.clearSession();
  }

  public requestPlayback(device: Device): void {
    this._sessionRepository.requestPlayback(device.id).then();
  }

  public openQueuePage(): void {
    this._router.navigate([PageRoutes.Queue])
      .then();
  }

  public openHistoryPage() {
    this._router.navigate([PageRoutes.History])
      .then();
  }
}
