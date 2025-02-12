import { Injectable } from '@angular/core';
import {CurrentPlaybackSessionRepositoryService} from "../repositories/current-playback-session-repository.service";
import {QueueRepositoryService} from "../repositories/queue-repository.service";
import {AudioElementRepositoryService} from "./audio-element-repository.service";
import {firstValueFrom} from "rxjs";
import {HistoryRepositoryService} from "../repositories/history-repository.service";
import {DeviceClient, UpdateDevicePlaybackCapabilitiesCommand} from "../../generated-clients/mix-server-clients";
import {ToastService} from "../toasts/toast-service";

@Injectable({
  providedIn: 'root'
})
export class AudioPlayerCapabilitiesService {
  constructor(private _audioElementRepository: AudioElementRepositoryService,
              private _devicesClient: DeviceClient,
              private _historyRepository: HistoryRepositoryService,
              private _playbackSessionRepository: CurrentPlaybackSessionRepositoryService,
              private _toastService: ToastService,
              private _queueRepository: QueueRepositoryService) {
    this._historyRepository.sessions$
      .subscribe(sessions => {
        const mimeTypes = [...new Set(sessions.map(m => m.currentNode.metadata.mimeType))]
        this.updateAudioCapabilities(mimeTypes);
      });

    this._playbackSessionRepository.currentSession$
      .subscribe(session => {
        if (session && session.currentNode.metadata instanceof MediaMetadata) {
          this.updateAudioCapabilities([session.currentNode.metadata.mimeType]);
        }
      });

    this._queueRepository.queue$()
      .subscribe(queue => {
        const mimeTypes = [...new Set(queue.items.map(m => m.file.metadata.mimeType))]
        this.updateAudioCapabilities(mimeTypes);
      });
  }

  private updateAudioCapabilities(mimeTypes: string[]) {
    const update: { [mimeType: string]: boolean } = {}
    mimeTypes.forEach(mimeType => {
      update[mimeType] = this._audioElementRepository.audio.canPlayType(mimeType) !== '';
    });

    firstValueFrom(this._devicesClient.updateDeviceCapabilities(new UpdateDevicePlaybackCapabilitiesCommand({
      capabilities: update
    }))).catch(
      e => this._toastService.logServerError(
        e, 'Failed to update device capabilities'
      )
    )
  }
}
