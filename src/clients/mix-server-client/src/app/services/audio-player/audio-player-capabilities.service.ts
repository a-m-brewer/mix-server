import { Injectable } from '@angular/core';
import {CurrentPlaybackSessionRepositoryService} from "../repositories/current-playback-session-repository.service";
import {QueueRepositoryService} from "../repositories/queue-repository.service";
import {AudioElementRepositoryService} from "./audio-element-repository.service";
import {firstValueFrom} from "rxjs";
import {HistoryRepositoryService} from "../repositories/history-repository.service";
import {DeviceClient, UpdateDevicePlaybackCapabilitiesCommand} from "../../generated-clients/mix-server-clients";
import {ToastService} from "../toasts/toast-service";
import {FileExplorerNodeRepositoryService} from "../repositories/file-explorer-node-repository.service";
import {FileExplorerFileNode} from "../../main-content/file-explorer/models/file-explorer-file-node";
import {AuthenticationService} from "../auth/authentication.service";

@Injectable({
  providedIn: 'root'
})
export class AudioPlayerCapabilitiesService {
  constructor(private authenticationService: AuthenticationService,
              private _audioElementRepository: AudioElementRepositoryService,
              private _devicesClient: DeviceClient,
              private _fileExplorer: FileExplorerNodeRepositoryService,
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

    this._fileExplorer.currentFolder$
      .subscribe(folder => {
        if (folder) {
          const files = folder.children.filter(f => f instanceof FileExplorerFileNode) as FileExplorerFileNode[];
          const mimeTypes = [...new Set(files.map(m => m.metadata.mimeType))]
          this.updateAudioCapabilities(mimeTypes);
        }
      });
  }

  private updateAudioCapabilities(mimeTypes: string[]) {
    if (!this.authenticationService.connected) {
      return
    }

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
