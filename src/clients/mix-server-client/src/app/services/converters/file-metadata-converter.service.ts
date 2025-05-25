import { Injectable } from '@angular/core';
import {FileMetadataResponse, MediaInfoDto, NodePathDto} from "../../generated-clients/mix-server-clients";
import {FileMetadata} from "../../main-content/file-explorer/models/file-metadata";
import {TracklistConverterService} from "./tracklist-converter.service";
import {MediaInfo} from "../../main-content/file-explorer/models/media-info";

@Injectable({
  providedIn: 'root'
})
export class FileMetadataConverterService {

  constructor(private _tracklistConverter: TracklistConverterService) { }

  public fromResponse(dto: FileMetadataResponse): FileMetadata {
    return new FileMetadata(
      dto.mimeType,
      dto.isMedia,
      !!dto.mediaInfo ? this.fromMediaInfoDto(dto.mediaInfo) : null,
      dto.transcodeStatus
    )
  }

  public fromMediaInfoDto(dto: MediaInfoDto): MediaInfo {
    return new MediaInfo(
      dto.duration,
      dto.bitrate,
      this._tracklistConverter.createTracklistForm(dto.tracklist)
    )
  }
}
