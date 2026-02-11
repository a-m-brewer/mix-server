import { Injectable } from '@angular/core';
import {PlaybackSession} from "./models/playback-session";
import {SessionApiService} from "../api.service";
import {PlaybackSessionConverterService} from "../converters/playback-session-converter.service";

@Injectable({
  providedIn: 'root'
})
export class HistoryRepositoryService {
  constructor(private _playbackSessionConverter: PlaybackSessionConverterService,
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

    return history.sessions.map(m => this._playbackSessionConverter.fromDto(m));
  }
}
