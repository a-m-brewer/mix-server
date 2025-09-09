import {CollectionViewer, DataSource} from "@angular/cdk/collections";
import {combineLatest, map, distinctUntilChanged, Subscription} from "rxjs";
import {QueueItem} from "../repositories/models/queue-item";
import {QueueRepositoryService} from "../repositories/queue-repository.service";

export class QueuePageDataSource extends DataSource<QueueItem> {
  private readonly _subscription = new Subscription();
  public pageSize = 25;
  private _currentLength = 0;
  private _isLoading = false;

  constructor(private _queueRepository: QueueRepositoryService) {
    super();
    this._queueRepository.requestInitialLoad();
  }

  connect(collectionViewer: CollectionViewer) {
    this.pageSize = this._queueRepository.pageSize;

    // Track current items length
    const items$ = this._queueRepository.queue$().pipe(
      map(q => q.items),
    );

    this._subscription.add(
      combineLatest([
        collectionViewer.viewChange.pipe(
          map(r => r.end),
          distinctUntilChanged()
        ),
        items$.pipe(
          map(items => items.length),
          distinctUntilChanged()
        )
      ]).subscribe(([end, length]) => {
        this._currentLength = length;

        // near-end prefetch threshold
        const threshold = Math.floor(this.pageSize / 2);

        if (!this._isLoading && end + threshold >= length) {
          this._isLoading = true;
          const start = length;
          const nextEnd = start + this.pageSize;

          // IMPORTANT: request indices beyond current length to append new data
          this._queueRepository.loadRange(start, nextEnd)
            .finally(() => this._isLoading = false);
        }
      })
    );

    return items$;
  }

  disconnect(): void {
    this._subscription.unsubscribe();
  }
}
