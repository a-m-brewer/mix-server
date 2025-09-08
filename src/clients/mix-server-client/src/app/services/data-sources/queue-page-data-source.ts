import {CollectionViewer, DataSource} from "@angular/cdk/collections";
import {map, Observable, Subscription} from "rxjs";
import {QueueItem} from "../repositories/models/queue-item";
import {QueueRepositoryService} from "../repositories/queue-repository.service";

export class QueuePageDataSource extends DataSource<QueueItem> {
  private readonly _subscription = new Subscription();

  public pageSize: number = 25;

  constructor(private _queueRepository: QueueRepositoryService) {
    super();
    this._queueRepository.requestInitialLoad();
  }

  connect(collectionViewer: CollectionViewer): Observable<QueueItem[]> {
    this.pageSize = this._queueRepository.pageSize;

    this._subscription.add(
      collectionViewer.viewChange.subscribe(range => {
        const pageStart = Math.floor(range.start / this.pageSize);
        const pageEnd = Math.floor(range.end / this.pageSize);

        for (let i = pageStart; i <= pageEnd; i++) {
          this._queueRepository.loadPage(i)
            .then();
        }
      })
    )

    return this._queueRepository.queue$()
      .pipe(map(queue => {
        return queue.flatChildren;
      }));
  }

  disconnect(collectionViewer: CollectionViewer): void {
    this._subscription.unsubscribe();
  }

}
