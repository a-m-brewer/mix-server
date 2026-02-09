import { DataSource, CollectionViewer } from '@angular/cdk/collections';
import { Observable, BehaviorSubject, Subscription } from 'rxjs';
import { debounceTime } from 'rxjs/operators';
import { QueueItem } from '../services/repositories/models/queue-item';

export class QueueDataSource extends DataSource<QueueItem | undefined> {
  private _cachedData = new Map<number, QueueItem>();
  private _fetchedRanges: { start: number; end: number }[] = [];
  private _dataStream$ = new BehaviorSubject<(QueueItem | undefined)[]>([]);
  private _subscription = new Subscription();
  private _totalCount = 0;
  private _hasMore = true;
  private _isFetching = false;
  private _fetchingRanges = new Set<string>();

  constructor(
    private _fetchRange: (start: number, end: number) => Promise<{ items: QueueItem[], totalCount: number }>
  ) {
    super();
  }

  get totalCount(): number {
    return this._totalCount;
  }

  get currentData(): (QueueItem | undefined)[] {
    return this._dataStream$.value;
  }

  connect(collectionViewer: CollectionViewer): Observable<(QueueItem | undefined)[]> {
    this._subscription.add(
      collectionViewer.viewChange
        .pipe(debounceTime(50))
        .subscribe(range => {
          const startIndex = range.start;
          const endIndex = range.end;

          if (!this._isRangeFetched(startIndex, endIndex) && this._hasMore && !this._isFetching) {
            this._fetchRangeData(startIndex, endIndex).then();
          }
        })
    );

    return this._dataStream$.asObservable();
  }

  disconnect(): void {
    this._subscription.unsubscribe();
  }

  async initialize(): Promise<void> {
    // Fetch initial data
    const initialData = await this._fetchRange(0, 50);

    if (initialData.items.length === 0) {
      this._hasMore = false;
      this._dataStream$.next([]);
      return;
    }

    this._totalCount = initialData.totalCount;

    // Cache initial data
    initialData.items.forEach((item, index) => {
      this._cachedData.set(index, item);
    });

    this._fetchedRanges.push({ start: 0, end: initialData.items.length });

    // If we got all items, there's no more data
    this._hasMore = initialData.items.length < this._totalCount;

    this._updateDataStream();
  }

  async reset(): Promise<void> {
    this._clearCache();
    await this.initialize();
  }

  private _clearCache(): void {
    this._cachedData.clear();
    this._fetchedRanges = [];
    this._totalCount = 0;
    this._hasMore = true;
    this._isFetching = false;
    this._fetchingRanges.clear();
  }

  private _isRangeFetched(start: number, end: number): boolean {
    return this._fetchedRanges.some(
      range => range.start <= start && range.end >= end
    );
  }

  private async _fetchRangeData(start: number, end: number): Promise<void> {
    // Prevent concurrent fetches
    if (this._isFetching) {
      return;
    }

    const rangeKey = `${start}-${end}`;

    // Check if this range is already being fetched
    if (this._fetchingRanges.has(rangeKey)) {
      return;
    }

    this._isFetching = true;
    this._fetchingRanges.add(rangeKey);

    try {
      // Calculate the exact range we need (not already cached)
      const fetchStart = start;
      const fetchEnd = Math.min(end, start + 50); // Limit to 50 items per fetch

      const data = await this._fetchRange(fetchStart, fetchEnd);

      if (data.items.length === 0) {
        this._hasMore = false;
        return;
      }

      // Update total count in case it changed
      this._totalCount = data.totalCount;

      // Cache the fetched data
      data.items.forEach((item, index) => {
        this._cachedData.set(fetchStart + index, item);
      });

      // Record the fetched range
      this._fetchedRanges.push({ start: fetchStart, end: fetchStart + data.items.length });
      this._mergeFetchedRanges();

      // If we've fetched everything, we've reached the end
      if (fetchStart + data.items.length >= this._totalCount) {
        this._hasMore = false;
      }

      // Update the data stream
      this._updateDataStream();
    } catch (error) {
      console.error('Error fetching range data:', error);
    } finally {
      this._fetchingRanges.delete(rangeKey);
      this._isFetching = false;
    }
  }

  private _mergeFetchedRanges(): void {
    // Sort ranges by start
    this._fetchedRanges.sort((a, b) => a.start - b.start);

    // Merge overlapping and adjacent ranges
    const merged: { start: number; end: number }[] = [];
    for (const range of this._fetchedRanges) {
      if (merged.length === 0) {
        merged.push(range);
        continue;
      }

      const last = merged[merged.length - 1];
      // Merge if ranges overlap or are adjacent
      if (range.start <= last.end + 1) {
        last.end = Math.max(last.end, range.end);
      } else {
        merged.push(range);
      }
    }

    this._fetchedRanges = merged;
  }

  private _updateDataStream(): void {
    const length = this._totalCount;

    const dataArray: (QueueItem | undefined)[] = new Array(length);

    this._cachedData.forEach((item, index) => {
      if (index < length) {
        dataArray[index] = item;
      }
    });

    this._dataStream$.next(dataArray);
  }
}
