import {Component, OnDestroy, OnInit} from '@angular/core';
import {ExpansionPanelComponent} from "../../components/controls/expansion-panel/expansion-panel.component";
import {AppModule} from "../../app.module";
import {SessionComponent} from "../audio-control/session/session.component";
import {Subject, takeUntil} from "rxjs";
import {
  CurrentPlaybackSessionRepositoryService
} from "../../services/repositories/current-playback-session-repository.service";
import {PlaybackSession} from "../../services/repositories/models/playback-session";
import {AsyncPipe, NgIf} from "@angular/common";
import {MatButtonModule} from "@angular/material/button";
import {MatIconModule} from "@angular/material/icon";
import {AudioPlayerService} from "../../services/audio-player/audio-player.service";
import {MatProgressBarModule} from "@angular/material/progress-bar";
import {AudioProgressSliderComponent} from "../audio-control/audio-progress-slider/audio-progress-slider.component";
import {AudioControlButtonsComponent} from "../audio-control/audio-control-buttons/audio-control-buttons.component";
import {SwitchDeviceMenuComponent} from "../audio-control/switch-device-menu/switch-device-menu.component";
import {Device} from "../../services/repositories/models/device";
import {DeviceRepositoryService} from "../../services/repositories/device-repository.service";

@Component({
  selector: 'app-audio-control-mobile',
  standalone: true,
  imports: [
    ExpansionPanelComponent,
    SessionComponent,
    NgIf,
    MatButtonModule,
    MatIconModule,
    AsyncPipe,
    MatProgressBarModule,
    AudioProgressSliderComponent,
    AudioControlButtonsComponent,
    SwitchDeviceMenuComponent
  ],
  templateUrl: './audio-control-mobile.component.html',
  styleUrl: './audio-control-mobile.component.scss'
})
export class AudioControlMobileComponent implements OnInit, OnDestroy {
  private _unsubscribe$ = new Subject();

  public expanded: boolean = false;
  public playbackSession?: PlaybackSession | null;
  public currentPlaybackDevice?: Device | null;
  public currentDevice?: Device | null;

  constructor(public audioPlayer: AudioPlayerService,
              private _deviceRepository: DeviceRepositoryService,
              private _playbackSessionRepository: CurrentPlaybackSessionRepositoryService) {
  }

  public ngOnInit(): void {
    this._playbackSessionRepository.currentSession$
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(playbackSession => {
        this.playbackSession = playbackSession;
      });

    this.audioPlayer.currentPlaybackDevice$
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(currentPlaybackDevice => {
        this.currentPlaybackDevice = currentPlaybackDevice;
      });

    this._deviceRepository.currentDevice$
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(currentDevice => {
        this.currentDevice = currentDevice;
      });
  }

  public ngOnDestroy(): void {
    this._unsubscribe$.next(null);
    this._unsubscribe$.complete();
  }

  public play(): void {
    this.audioPlayer.requestPlaybackOnCurrentPlaybackDevice().then();
  }

  public pause(): void {
    this.audioPlayer.requestPause();
  }

  public onExpanded(expanded: boolean) {
    this.expanded = expanded;
  }
}
