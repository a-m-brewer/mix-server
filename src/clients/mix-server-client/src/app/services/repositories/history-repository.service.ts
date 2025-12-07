import { Injectable } from '@angular/core';
import {PlaybackSession} from "./models/playback-session";
import {NodeCacheService} from "../nodes/node-cache.service";
import {SessionApiService} from "../api.service";
import {PlaybackSessionConverterService} from "../converters/playback-session-converter.service";

const DEFAULT_VIRTUAL_SCROLL_LENGTH = 1000;

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

  public async getTotalCount(): Promise<number> {
    // Fetch a small initial range to determine if there are items
    const result = await this._sessionClient.request(
      'GetInitialHistoryLength',
      c => c.history(0, 1),
      'Failed to fetch history');

    // If we get results, we assume there are many more
    // The actual count will be determined by scrolling
    if (result.result && result.result.sessions.length > 0) {
      return DEFAULT_VIRTUAL_SCROLL_LENGTH;
    }

    return 0;
  }
}
