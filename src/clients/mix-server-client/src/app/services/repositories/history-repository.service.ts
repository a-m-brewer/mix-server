import { Injectable } from '@angular/core';
import {PlaybackSession} from "./models/playback-session";
import {BehaviorSubject, Observable} from "rxjs";
import {AuthenticationService} from "../auth/authentication.service";
import {PlaybackSessionConverterService} from "../converters/playback-session-converter.service";
import {cloneDeep} from "lodash";
import {NodeCacheService} from "../nodes/node-cache.service";
import {SessionApiService} from "../api.service";

@Injectable({
  providedIn: 'root'
})
export class HistoryRepositoryService {
  private _sessions$ = new BehaviorSubject<Array<PlaybackSession>>([]);
  private _moreItemsAvailable$ = new BehaviorSubject<boolean>(true);

  constructor(private _nodeCache: NodeCacheService,
              private _playbackSessionConverter: PlaybackSessionConverterService,
              private _sessionClient: SessionApiService) {
  }

  public get sessions$(): Observable<Array<PlaybackSession>> {
    return this._sessions$.asObservable();
  }

  public get moreItemsAvailable$(): Observable<boolean> {
    return this._moreItemsAvailable$.asObservable();
  }

  public async loadMoreItems(): Promise<void> {
    if (!this._moreItemsAvailable$.value) {
      return;
    }

    const previousSessionHistory = cloneDeep(this._sessions$.value);

    const loadingId = `LoadMoreHistoryItems-${previousSessionHistory.length}`;
    const pageSize = 15;
    const result = await this._sessionClient.request(
      loadingId,
        c => c.history(previousSessionHistory.length, pageSize),
      'Failed to fetch history');

    const history = result.result;
    if (!history) {
      return;
    }

    this._moreItemsAvailable$.next(history.sessions.length === pageSize);

    if (history.sessions.length > 0) {
      const nextSessionHistory = [
        ...previousSessionHistory,
        ...history.sessions.map(m => this._playbackSessionConverter.fromDto(m))
      ];
      this.next(nextSessionHistory);
    }
  }

  private next(sessions: PlaybackSession[]) {
    const folders = [...new Set(sessions.map(session => session.currentNode.parent.path))];
    folders.forEach(folder => {
      void this._nodeCache.loadDirectory(folder)
    })

    this._sessions$.value.forEach(session => {
      session.destroy();
    });

    this._sessions$.next(sessions)
  }
}
