import {Component, OnDestroy, OnInit} from '@angular/core';
import {ExpansionPanelComponent} from "../../components/controls/expansion-panel/expansion-panel.component";
import {AppModule} from "../../app.module";
import {SessionComponent} from "../audio-control/session/session.component";
import {distinctUntilChanged, Subject, takeUntil} from "rxjs";
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
import {LoadingFabIconComponent} from "../../components/controls/loading-fab-icon/loading-fab-icon.component";
import {LoadingRepositoryService} from "../../services/repositories/loading-repository.service";

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
    SwitchDeviceMenuComponent,
    LoadingFabIconComponent
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
  public playLoading: boolean = false;
  public currentlyPlayingTrackInfo?: string;

  constructor(public audioPlayer: AudioPlayerService,
              private _deviceRepository: DeviceRepositoryService,
              private _loadingRepository: LoadingRepositoryService,
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

    this.audioPlayer.currentCue$
      .pipe(
        takeUntil(this._unsubscribe$),
        distinctUntilChanged((prev, curr) => prev?.cue === curr?.cue)
      )
      .subscribe(cue => {
        this.currentlyPlayingTrackInfo = (cue?.tracks ?? []).map(track => `${track.name} - ${track.artist}`).join(', ');
      });

    this._deviceRepository.currentDevice$
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(currentDevice => {
        this.currentDevice = currentDevice;
      });

    this._loadingRepository
      .status$()
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(status => {
        this.playLoading = status.isLoadingAction('RequestPlayback')
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
