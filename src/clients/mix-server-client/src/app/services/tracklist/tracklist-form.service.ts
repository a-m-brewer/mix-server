import { Injectable } from '@angular/core';
import {
  SaveTracklistCommand,
  TracklistClient,
} from "../../generated-clients/mix-server-clients";
import {LoadingRepositoryService} from "../repositories/loading-repository.service";
import {ToastService} from "../toasts/toast-service";
import {firstValueFrom, Observable} from "rxjs";
import {CurrentPlaybackSessionRepositoryService} from "../repositories/current-playback-session-repository.service";
import {TracklistConverterService} from "../converters/tracklist-converter.service";
import {MediaMetadata} from "../../main-content/file-explorer/models/media-metadata";

@Injectable({
  providedIn: 'root'
})
export class TracklistFormService {
  constructor(private _client: TracklistClient,
              private _loading: LoadingRepositoryService,
              private _sessionRepository: CurrentPlaybackSessionRepositoryService,
              private _toastService: ToastService,
              private _tracklistConverter: TracklistConverterService) {
  }

  public async importTracklistFile(file: File): Promise<void> {
    this._loading.startLoading();

    try {
      const dto = await firstValueFrom(this._client.importTracklist({
        fileName: file.name,
        data: file as Blob
      }));

      this._sessionRepository.updateCurrentSessionTracklist(dto.tracklist, true);
    } catch (err) {
      this._toastService.logServerError(err, 'Failed to import tracklist file');
    } finally {
      this._loading.stopLoading();
    }
  }


  public async saveTracklist(): Promise<void> {
    const currentTracklist = this._sessionRepository.currentSession?.currentNode?.metadata instanceof MediaMetadata
      ? this._sessionRepository.currentSession.currentNode.metadata.tracklist
      : undefined;
    if (!currentTracklist) {
      return;
    }

    this._loading.startLoading();

    try {
      const dto = await firstValueFrom(this._client.saveTracklist(new SaveTracklistCommand({
        tracklist: this._tracklistConverter.convertFormToDto(currentTracklist.controls.cues)
      })));
      this._sessionRepository.updateCurrentSessionTracklist(dto.tracklist, false);
    } catch (err) {
      this._toastService.logServerError(err, 'Failed to save tracklist');
    } finally {
      this._loading.stopLoading();
    }
  }
}
