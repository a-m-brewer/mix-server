import { Injectable } from '@angular/core';
import {CurrentPlaybackSessionRepositoryService} from "../repositories/current-playback-session-repository.service";
import {QueueRepositoryService} from "../repositories/queue-repository.service";
import {AudioElementRepositoryService} from "./audio-element-repository.service";
import {distinctUntilChanged, Subject} from "rxjs";
import {HistoryRepositoryService} from "../repositories/history-repository.service";
import {UpdateDevicePlaybackCapabilitiesCommand} from "../../generated-clients/mix-server-clients";
import {ToastService} from "../toasts/toast-service";
import {FileExplorerNodeRepositoryService} from "../repositories/file-explorer-node-repository.service";
import {FileExplorerFileNode} from "../../main-content/file-explorer/models/file-explorer-file-node";
import {AuthenticationService} from "../auth/authentication.service";
import sameCapabilities from "./same-capabilities";
import {DeviceApiService} from "../api.service";

@Injectable({
  providedIn: 'root'
})
export class AudioPlayerCapabilitiesService {
  private _capabilitiesCache: { [mimeType: string]: boolean } = {};
  private _requests$ = new Subject<{[mimeType: string]: boolean}>();

  constructor(private authenticationService: AuthenticationService,
              private _audioElementRepository: AudioElementRepositoryService,
              private _devicesClient: DeviceApiService,
              private _fileExplorer: FileExplorerNodeRepositoryService,
              private _historyRepository: HistoryRepositoryService,
              private _playbackSessionRepository: CurrentPlaybackSessionRepositoryService,
              private _queueRepository: QueueRepositoryService) {
  }

  public initialize() {
    this._requests$
      .pipe(distinctUntilChanged((prev, next) => sameCapabilities(prev, next)))
      .subscribe(capabilities => {
        this._devicesClient.request('UpdateDevicePlaybackCapabilities',
          client => client.updateDeviceCapabilities(new UpdateDevicePlaybackCapabilitiesCommand({
            capabilities
          })), 'Error updating device capabilities', {
            triggerLoading: false
          })
          .then();
      })

    this.authenticationService.connected$
      .subscribe(connected => {
        if (connected) {
          this.updateAudioCapabilities([]);
        }
      });

    this._historyRepository.sessions$
      .subscribe(pagedSession => {
        const mimeTypes = [...new Set(pagedSession.flatChildren.map(m => m.currentNode.metadata.mimeType))]
        this.updateAudioCapabilities(mimeTypes);
      });

    this._playbackSessionRepository.currentSession$
      .subscribe(session => {
        if (session && session.currentNode.metadata.isMedia) {
          this.updateAudioCapabilities([session.currentNode.metadata.mimeType]);
        }
      });

    this._queueRepository.queue$()
      .subscribe(queue => {
        const mimeTypes = [...new Set(queue.flatChildren.map(m => m.file.metadata.mimeType))]
        this.updateAudioCapabilities(mimeTypes);
      });

    this._fileExplorer.currentFolder$
      .subscribe(folder => {
        if (folder) {
          const files = folder.flatChildren.filter(f => f instanceof FileExplorerFileNode) as FileExplorerFileNode[];
          const mimeTypes = [...new Set(files.map(m => m.metadata.mimeType))]
          this.updateAudioCapabilities(mimeTypes);
        }
      });
  }

  private updateAudioCapabilities(mimeTypes: string[]) {
    const update: { [mimeType: string]: boolean } = {}
    mimeTypes.forEach(mimeType => {
      update[mimeType] = this._audioElementRepository.canPlayType(mimeType) !== '';
    });

    this._capabilitiesCache = {...this._capabilitiesCache, ...update};

    if (!this.authenticationService.connected) {
      return
    }

    this._requests$.next(this._capabilitiesCache);
  }
}
