import { Injectable } from '@angular/core';
import {FileMetadataResponse, MediaMetadataResponse} from "../../generated-clients/mix-server-clients";
import {FileMetadata} from "../../main-content/file-explorer/models/file-metadata";
import {TracklistConverterService} from "./tracklist-converter.service";
import {MediaMetadata} from "../../main-content/file-explorer/models/media-metadata";

@Injectable({
  providedIn: 'root'
})
export class FileMetadataConverterService {

  constructor(private _tracklistConverter: TracklistConverterService) { }

  public fromResponse(dto: FileMetadataResponse): FileMetadata {
    if (!(dto instanceof MediaMetadataResponse)) {
      return new FileMetadata(dto.mimeType);
    }

    return new MediaMetadata(
      dto.mimeType,
      dto.duration,
      dto.bitrate,
      dto.transcodeState,
      this._tracklistConverter.createTracklistForm(dto.tracklist)
    )
  }
}
