import { Injectable } from '@angular/core';
import {PlaybackSession} from "./models/playback-session";
import {BehaviorSubject, combineLatestWith, Observable, Subject} from "rxjs";
import {AuthenticationService} from "../auth/authentication.service";
import {PlaybackSessionConverterService} from "../converters/playback-session-converter.service";
import {cloneDeep} from "lodash";
import {NodeCacheService} from "../nodes/node-cache.service";
import {SessionApiService} from "../api.service";
import {PagedSessions} from "./models/paged-sessions";

@Injectable({
  providedIn: 'root'
})
export class HistoryRepositoryService {
  private _initialLoadRequested$ = new BehaviorSubject<boolean>(false);
  private _sessions$ = new BehaviorSubject<PagedSessions>(PagedSessions.Default);

  constructor(private _authService: AuthenticationService,
              private _nodeCache: NodeCacheService,
              private _playbackSessionConverter: PlaybackSessionConverterService,
              private _sessionClient: SessionApiService) {
    this._authService.connected$
      .pipe(combineLatestWith(this._initialLoadRequested$))
      .subscribe(([connected, initialLoadRequested]) => {
        if (connected && initialLoadRequested) {
          this.loadPage(0).then();
        }
      });
  }

  public pageSize = 25;

  public get sessions$(): Observable<PagedSessions> {
    return this._sessions$.asObservable();
  }

  public requestInitialLoad(): void {
    if (this._initialLoadRequested$.value) {
      return;
    }

    this._initialLoadRequested$.next(true);
  }

  public async loadPage(pageIndex: number): Promise<void> {
    const loadingId = `LoadHistoryPage-${pageIndex}`;

    const result = await this._sessionClient.request(
      loadingId,
      c => c.history(pageIndex, this.pageSize),
      'Failed to fetch history'
    )

    if (!result.result) {
      return;
    }

    const pageItems = result.result.sessions.map(m => this._playbackSessionConverter.fromDto(m));

    const next = this._sessions$.value.copy();

    next.addPage(pageIndex, pageItems);

    this.next(next);
  }

  private next(pagedSessions: PagedSessions): void {
    const folders = [...new Set(pagedSessions.flatChildren.map(session => session.currentNode.parent.path))];
    this._nodeCache.loadDirectoriesForConsumer("history", folders).then();

    this._sessions$.value.flatChildren.forEach(session => {
      session.destroy();
    });

    this._sessions$.next(pagedSessions);
  }
}
