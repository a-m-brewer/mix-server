import { Injectable } from '@angular/core';
import {PlaybackSessionDto, PlaybackStateDto} from "../../generated-clients/mix-server-clients";
import {PlaybackSession} from "../repositories/models/playback-session";
import {FileExplorerNodeConverterService} from "./file-explorer-node-converter.service";
import {PlaybackState} from "../repositories/models/playback-state";

@Injectable({
  providedIn: 'root'
})
export class PlaybackSessionConverterService {

  constructor(private _fileExplorerNodeConverter: FileExplorerNodeConverterService) { }

  public fromDto(dto: PlaybackSessionDto): PlaybackSession {
    return new PlaybackSession(
      dto.id,
      this._fileExplorerNodeConverter.fromFileExplorerFileNode(dto.file),
      dto.lastPlayed,
      this.stateFromSessionDto(dto),
      dto.autoPlay);
  }

  public stateFromSessionDto(dto: PlaybackSessionDto): PlaybackState {
    return new PlaybackState(dto.currentTime, dto.deviceId, dto.playing);
  }

  public fromStateDto(dto: PlaybackStateDto): PlaybackState {
    return new PlaybackState(dto.currentTime, dto.deviceId, dto.playing);
  }
}
