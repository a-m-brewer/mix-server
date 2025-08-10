import {CollectionViewer, DataSource} from "@angular/cdk/collections";
import {PlaybackSession} from "../repositories/models/playback-session";
import {map, Observable, Subscription} from "rxjs";
import {HistoryRepositoryService} from "../repositories/history-repository.service";

export class HistoryPageDataSource extends DataSource<PlaybackSession> {
  private readonly _subscription = new Subscription();

  constructor(private _historyRepository: HistoryRepositoryService) {
    super();
    this._historyRepository.requestInitialLoad();
  }

  public pageSize: number = 25;

  connect(collectionViewer: CollectionViewer): Observable<PlaybackSession[]> {
    this.pageSize = this._historyRepository.pageSize

    this._subscription.add(
      collectionViewer.viewChange.subscribe(range => {
        const pageStart = Math.floor(range.start / this.pageSize);
        const pageEnd = Math.floor(range.end / this.pageSize);

        for (let i = pageStart; i <= pageEnd; i++) {
          this._historyRepository.loadPage(i)
            .then();
        }
      })
    )

    return this._historyRepository.sessions$
      .pipe(
        map(sessions => {
          return sessions.flatChildren;
        })
      );
  }

  disconnect(collectionViewer: CollectionViewer): void {
    this._subscription.unsubscribe();
  }

}
