import { Injectable } from '@angular/core';
import {PlaybackSession} from "./models/playback-session";
import {NodeCacheService} from "../nodes/node-cache.service";
import {SessionApiService} from "../api.service";
import {PlaybackSessionConverterService} from "../converters/playback-session-converter.service";

@Injectable({
  providedIn: 'root'
})
export class HistoryRepositoryService {
  constructor(private _nodeCache: NodeCacheService,
              private _playbackSessionConverter: PlaybackSessionConverterService,
              private _sessionClient: SessionApiService) {
  }

  public async fetchRange(startIndex: number, endIndex: number): Promise<PlaybackSession[]> {
    const loadingId = `LoadHistoryRange-${startIndex}-${endIndex}`;
    const result = await this._sessionClient.request(
      loadingId,
      c => c.history(startIndex, endIndex),
      'Failed to fetch history');

    const history = result.result;
    if (!history || !history.sessions) {
      return [];
    }

    const sessions = history.sessions.map(m => this._playbackSessionConverter.fromDto(m));

    // Pre-load directories for these sessions
    const folders = [...new Set(sessions.map(session => session.currentNode.parent.path))];
    folders.forEach(folder => {
      void this._nodeCache.loadDirectory(folder)
    });

    return sessions;
  }
}
