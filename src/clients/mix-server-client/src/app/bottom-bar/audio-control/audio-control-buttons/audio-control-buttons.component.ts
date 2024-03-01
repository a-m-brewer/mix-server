import { Component } from '@angular/core';
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

@Component({
  selector: 'app-audio-control-buttons, [app-audio-control-buttons]',
  standalone: true,
  imports: [
    AsyncPipe,
    MatButtonModule,
    MatIconModule,
    AudioContextMenuComponent,
    SwitchDeviceMenuComponent
  ],
  templateUrl: './audio-control-buttons.component.html',
  styleUrl: './audio-control-buttons.component.scss'
})
export class AudioControlButtonsComponent {
  constructor(public audioPlayer: AudioPlayerService,
              private _sessionService: SessionService,
              private _queueRepository: QueueRepositoryService) {
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
