import {Component, OnDestroy, OnInit} from '@angular/core';
import {AsyncPipe} from "@angular/common";
import {MatButtonModule} from "@angular/material/button";
import {MatIconModule} from "@angular/material/icon";
import {AudioPlayerService} from "../../../services/audio-player/audio-player.service";
import {AudioContextMenuComponent} from "../audio-context-menu/audio-context-menu.component";
import {QueueRepositoryService} from "../../../services/repositories/queue-repository.service";
import {
  CurrentPlaybackSessionRepositoryService
} from "../../../services/repositories/current-playback-session-repository.service";
import {SwitchDeviceMenuComponent} from "../switch-device-menu/switch-device-menu.component";
import {SessionService} from "../../../services/sessions/session.service";
import {LoadingFabIconComponent} from "../../../components/controls/loading-fab-icon/loading-fab-icon.component";
import {LoadingRepositoryService} from "../../../services/repositories/loading-repository.service";
import {Subject, takeUntil} from 'rxjs';

@Component({
  selector: 'app-audio-control-buttons, [app-audio-control-buttons]',
  standalone: true,
  imports: [
    AsyncPipe,
    MatButtonModule,
    MatIconModule,
    AudioContextMenuComponent,
    SwitchDeviceMenuComponent,
    LoadingFabIconComponent
  ],
  templateUrl: './audio-control-buttons.component.html',
  styleUrl: './audio-control-buttons.component.scss'
})
export class AudioControlButtonsComponent implements OnInit, OnDestroy{
  private _unsubscribe$: Subject<void> = new Subject<void>();

  public playLoading: boolean = false;

  constructor(public audioPlayer: AudioPlayerService,
              private _loadingRepository: LoadingRepositoryService,
              private _sessionService: SessionService,
              private _queueRepository: QueueRepositoryService) {
  }

  public ngOnInit(): void {
    this._loadingRepository
      .status$()
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(status => {
        this.playLoading = status.isLoadingAction('RequestPlayback')
      });
  }

  public ngOnDestroy(): void {
    this._unsubscribe$.next();
    this._unsubscribe$.complete();
  }

  public play(): void {
    this.audioPlayer.requestPlaybackOnCurrentPlaybackDevice().then();
  }

  public pause(): void {
    this.audioPlayer.requestPause();
  }

  public skipPrevious(): void {
    if (!this._queueRepository.previousQueueItem) {
      return;
    }

    this._sessionService.back();
  }

  public skipNext(): void {
    if (!this._queueRepository.nextQueueItem) {
      return;
    }

    this._sessionService.skip();
  }

  public backward(): void {
    this.audioPlayer.seekOffset(-30);
  }

  public forward(): void {
    this.audioPlayer.seekOffset(30);
  }
}
