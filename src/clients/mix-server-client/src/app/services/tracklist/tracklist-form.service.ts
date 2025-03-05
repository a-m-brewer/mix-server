import { Injectable } from '@angular/core';
import {
  SaveTracklistCommand,
} from "../../generated-clients/mix-server-clients";
import {CurrentPlaybackSessionRepositoryService} from "../repositories/current-playback-session-repository.service";
import {TracklistConverterService} from "../converters/tracklist-converter.service";
import {TracklistApiService} from "../api.service";

@Injectable({
  providedIn: 'root'
})
export class TracklistFormService {
  constructor(private _client: TracklistApiService,
              private _sessionRepository: CurrentPlaybackSessionRepositoryService,
              private _tracklistConverter: TracklistConverterService) {
  }

  public async importTracklistFile(file: File): Promise<void> {
    const result = await this._client.request('ImportTracklist',
      client => client.importTracklist({
        fileName: file.name,
        data: file as Blob
      }), 'Failed to import tracklist file');

    result.success(dto => this._sessionRepository.updateCurrentSessionTracklist(dto.tracklist, true));
  }


  public async saveTracklist(): Promise<void> {
    const currentTracklist = !!this._sessionRepository.currentSession?.currentNode?.metadata.mediaInfo
      ? this._sessionRepository.currentSession.currentNode.metadata.mediaInfo.tracklist
      : undefined;
    if (!currentTracklist) {
      return;
    }

    const result = await this._client.request('SaveTracklist',
      client => client.saveTracklist(new SaveTracklistCommand({
        tracklist: this._tracklistConverter.convertFormToDto(currentTracklist.controls.cues)
      })), 'Failed to save tracklist');

    result.success(dto => this._sessionRepository.updateCurrentSessionTracklist(dto.tracklist, false));
  }
}
