import { Injectable } from '@angular/core';
import {PlaybackGrantedDto, PlaybackSessionDto, PlaybackStateDto} from "../../generated-clients/mix-server-clients";
import {PlaybackSession} from "../repositories/models/playback-session";
import {FileExplorerNodeConverterService} from "./file-explorer-node-converter.service";
import {PlaybackState} from "../repositories/models/playback-state";
import {PlaybackGranted} from "../repositories/models/playback-granted";
import {TracklistConverterService} from "./tracklist-converter.service";
import {NodeCacheService} from "../nodes/node-cache.service";

@Injectable({
  providedIn: 'root'
})
export class PlaybackSessionConverterService {

  constructor(private _fileExplorerNodeConverter: FileExplorerNodeConverterService,
              private _nodeCache: NodeCacheService) { }

  public fromDto(dto: PlaybackSessionDto): PlaybackSession {
    const initialNode = this._fileExplorerNodeConverter.fromFileExplorerFileNode(dto.file)
    return new PlaybackSession(
      dto.id,
      initialNode,
      this._nodeCache.getFileByNode$(initialNode),
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

  public fromPlaybackGrantedDto(dto: PlaybackGrantedDto): PlaybackGranted {
    return new PlaybackGranted(dto.currentTime, dto.deviceId, dto.playing, dto.useDeviceCurrentTime);
  }
}
