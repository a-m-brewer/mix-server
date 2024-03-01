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
import {MatButtonModule} from "@angular/material/button";
import {MatMenuModule} from "@angular/material/menu";
import {MatTooltipModule} from "@angular/material/tooltip";
import {NgForOf} from "@angular/common";
import {MatIconModule} from "@angular/material/icon";
import {SwitchDeviceMenuComponent} from "../switch-device-menu/switch-device-menu.component";
import {SessionService} from "../../../services/sessions/session.service";

@Component({
  selector: 'app-audio-context-menu',
  templateUrl: './audio-context-menu.component.html',
  standalone: true,
  imports: [
    MatButtonModule,
    MatIconModule,
    MatMenuModule,
    MatTooltipModule,
    NgForOf
  ],
  styleUrls: ['./audio-context-menu.component.scss']
})
export class AudioContextMenuComponent implements OnInit, OnDestroy{
  private _unsubscribe$ = new Subject();

  public session: PlaybackSession | null = null;
  public disconnected: boolean = true;

  constructor(
    private _authService: AuthenticationService,
    private _router: Router,
    private _sessionRepository: CurrentPlaybackSessionRepositoryService,
    private _sessionService: SessionService) {
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
  }

  public ngOnDestroy(): void {
    this._unsubscribe$.next(null);
    this._unsubscribe$.complete();
  }

  public clearSession(): void {
    this._sessionService.clearSession();
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
