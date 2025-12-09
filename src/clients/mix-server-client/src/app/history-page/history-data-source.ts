import { DataSource, CollectionViewer } from '@angular/cdk/collections';
import { Observable, BehaviorSubject, Subscription } from 'rxjs';
import { debounceTime } from 'rxjs/operators';
import { PlaybackSession } from '../services/repositories/models/playback-session';

export class HistoryDataSource extends DataSource<PlaybackSession | undefined> {
  private _cachedData = new Map<number, PlaybackSession>();
  private _fetchedRanges: { start: number; end: number }[] = [];
  private _dataStream$ = new BehaviorSubject<(PlaybackSession | undefined)[]>([]);
  private _subscription = new Subscription();
  private _hasMore = true;
  private _fetchingRanges = new Set<string>();
  private _isFetching = false;

  constructor(
    private _fetchRange: (start: number, end: number) => Promise<PlaybackSession[]>
  ) {
    super();
  }

  connect(collectionViewer: CollectionViewer): Observable<(PlaybackSession | undefined)[]> {
    this._subscription.add(
      collectionViewer.viewChange
        .pipe(debounceTime(50)) // Debounce to avoid rapid-fire requests
        .subscribe(range => {
          const startIndex = range.start;
          const endIndex = range.end;

          // Check if we need to fetch this range
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

  get currentData(): (PlaybackSession | undefined)[] {
    return this._dataStream$.value;
  }

  async initialize(): Promise<void> {
    // Fetch initial data
    const initialData = await this._fetchRange(0, 50);

    if (initialData.length === 0) {
      this._hasMore = false;
      this._dataStream$.next([]);
      return;
    }

    // Cache initial data
    initialData.forEach((session, index) => {
      this._cachedData.set(index, session);
    });

    this._fetchedRanges.push({ start: 0, end: initialData.length });

    // If we got less than requested, there's no more data
    this._hasMore = initialData.length === 50;

    this._updateDataStream();
  }

  async reset(): Promise<void> {
    this._cachedData.clear();
    this._fetchedRanges = [];
    await this.initialize();
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

      if (data.length === 0) {
        this._hasMore = false;
        return;
      }

      // Cache the fetched data
      data.forEach((session, index) => {
        this._cachedData.set(fetchStart + index, session);
      });

      // Record the fetched range
      this._fetchedRanges.push({ start: fetchStart, end: fetchStart + data.length });
      this._mergeFetchedRanges();

      // If we got less data than requested, we've reached the end
      if (data.length < (fetchEnd - fetchStart)) {
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
    // Create array with length = highest cached index + buffer
    const maxIndex = Math.max(...Array.from(this._cachedData.keys()), -1);
    const length = this._hasMore ? maxIndex + 50 : maxIndex + 1; // Add modest buffer if more data expected

    const dataArray: (PlaybackSession | undefined)[] = new Array(length);

    this._cachedData.forEach((session, index) => {
      dataArray[index] = session;
    });

    this._dataStream$.next(dataArray);
  }

  // Handle live updates
  handleNewSession(session: PlaybackSession): void {
    // Check if this session already exists in the cache
    let existingIndex = -1;
    for (const [index, cachedSession] of this._cachedData.entries()) {
      if (cachedSession.id === session.id) {
        existingIndex = index;
        break;
      }
    }

    // Build new cache with existing items, excluding the duplicate
    const newCache = new Map<number, PlaybackSession>();
    newCache.set(0, session);

    let newIndex = 1;
    this._cachedData.forEach((value, key) => {
      // Skip the existing session if it was found
      if (key !== existingIndex) {
        newCache.set(newIndex, value);
        newIndex++;
      }
    });

    this._cachedData = newCache;

    // Update ranges to account for shifted indices
    this._fetchedRanges = this._fetchedRanges.map(range => {
      let newStart = range.start + 1;
      let newEnd = range.end + 1;

      // Adjust for removed item if it was within this range
      if (existingIndex !== -1 && existingIndex >= range.start && existingIndex < range.end) {
        newEnd--;
      }

      return { start: newStart, end: newEnd };
    });

    // Add the new first item as a fetched range
    this._fetchedRanges.unshift({ start: 0, end: 1 });
    this._mergeFetchedRanges();


    this._updateDataStream();
  }
}
