import { Injectable } from '@angular/core';
import {PlaybackSession} from "./models/playback-session";
import {BehaviorSubject, firstValueFrom, Observable} from "rxjs";
import {GetUsersSessionsResponse, SessionClient} from "../../generated-clients/mix-server-clients";
import {AuthenticationService} from "../auth/authentication.service";
import {LoadingRepositoryService} from "./loading-repository.service";
import {ToastService} from "../toasts/toast-service";
import {PlaybackSessionConverterService} from "../converters/playback-session-converter.service";

@Injectable({
  providedIn: 'root'
})
export class HistoryRepositoryService {
  private _sessions$ = new BehaviorSubject<Array<PlaybackSession>>([]);
  private _moreItemsAvailable$ = new BehaviorSubject<boolean>(true);

  constructor(authenticationService: AuthenticationService,
              private _loadingRepository: LoadingRepositoryService,
              private _playbackSessionConverter: PlaybackSessionConverterService,
              private _sessionClient: SessionClient,
              private _toastService: ToastService) {
    authenticationService.connected$
      .subscribe(connected => {
        if (connected) {
          this.loadMoreItems().then();
        }
      })
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

    this._loadingRepository.startLoading();

    const previousSessionHistory = this._sessions$.value;

    const pageSize = 15;
    const history = await firstValueFrom(this._sessionClient.history(previousSessionHistory.length, pageSize))
      .catch(err => {
        this._toastService.logServerError(err, 'Failed to fetch history');
        return new GetUsersSessionsResponse();
      });

    this._moreItemsAvailable$.next(history.sessions.length === pageSize);

    if (history.sessions.length > 0) {
      const nextSessionHistory = [
        ...previousSessionHistory,
        ...history.sessions.map(m => this._playbackSessionConverter.fromDto(m))
      ];
      this._sessions$.next(nextSessionHistory);
    }

    this._loadingRepository.stopLoading();
  }
}
