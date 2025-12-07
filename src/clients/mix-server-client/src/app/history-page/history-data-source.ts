import { DataSource, CollectionViewer } from '@angular/cdk/collections';
import { Observable, BehaviorSubject, Subscription } from 'rxjs';
import { PlaybackSession } from '../services/repositories/models/playback-session';

export class HistoryDataSource extends DataSource<PlaybackSession> {
  private _cachedData = new Map<number, PlaybackSession>();
  private _fetchedRanges: { start: number; end: number }[] = [];
  private _dataStream$ = new BehaviorSubject<PlaybackSession[]>([]);
  private _subscription = new Subscription();
  private _totalLength = 0;

  constructor(
    private _fetchRange: (start: number, end: number) => Promise<PlaybackSession[]>,
    private _getTotalCount: () => Promise<number>
  ) {
    super();
  }

  connect(collectionViewer: CollectionViewer): Observable<PlaybackSession[]> {
    this._subscription.add(
      collectionViewer.viewChange.subscribe(range => {
        const startIndex = range.start;
        const endIndex = Math.min(range.end, this._totalLength);
        
        // Check if we need to fetch this range
        if (endIndex > startIndex && !this._isRangeFetched(startIndex, endIndex)) {
          this._fetchRangeData(startIndex, endIndex).then();
        }
      })
    );

    return this._dataStream$.asObservable();
  }

  disconnect(): void {
    this._subscription.unsubscribe();
  }

  get currentData(): PlaybackSession[] {
    return this._dataStream$.value;
  }

  async initialize(): Promise<void> {
    // Fetch initial data to determine actual count
    const initialData = await this._fetchRange(0, 50);
    
    if (initialData.length === 0) {
      this._totalLength = 0;
      this._dataStream$.next([]);
      return;
    }
    
    // If we got full page, there's likely more
    const estimatedTotal = await this._getTotalCount();
    this._totalLength = estimatedTotal;
    
    // Cache initial data
    initialData.forEach((session, index) => {
      this._cachedData.set(index, session);
    });
    
    this._fetchedRanges.push({ start: 0, end: initialData.length });
    this._updateDataStream();
  }

  async reset(): Promise<void> {
    this._cachedData.clear();
    this._fetchedRanges = [];
    await this.initialize();
  }

  private _isRangeFetched(start: number, end: number): boolean {
    // Check if entire range is covered by fetched ranges
    let currentPos = start;
    const sortedRanges = [...this._fetchedRanges].sort((a, b) => a.start - b.start);
    
    for (const range of sortedRanges) {
      if (range.start <= currentPos && range.end > currentPos) {
        currentPos = range.end;
        if (currentPos >= end) {
          return true;
        }
      }
    }
    
    return currentPos >= end;
  }

  private async _fetchRangeData(start: number, end: number): Promise<void> {
    try {
      const data = await this._fetchRange(start, end);
      
      if (data.length === 0) {
        // No more data available, update total length
        this._totalLength = Math.max(start, this._cachedData.size);
        return;
      }
      
      // Cache the fetched data
      data.forEach((session, index) => {
        this._cachedData.set(start + index, session);
      });

      // Record the fetched range
      this._fetchedRanges.push({ start, end: start + data.length });
      this._mergeFetchedRanges();

      // Update total if we have more data than expected
      const maxIndex = Math.max(...Array.from(this._cachedData.keys()));
      if (maxIndex + 1 > this._totalLength) {
        this._totalLength = maxIndex + 1;
      }

      // Update the data stream
      this._updateDataStream();
    } catch (error) {
      console.error('Error fetching range data:', error);
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
    // Build contiguous array from cached data
    // Only include items that are actually cached to avoid empty space
    const dataArray: PlaybackSession[] = new Array(this._totalLength);
    
    for (let i = 0; i < this._totalLength; i++) {
      const session = this._cachedData.get(i);
      if (session) {
        dataArray[i] = session;
      }
    }

    this._dataStream$.next(dataArray);
  }

  // Handle live updates
  handleNewSession(session: PlaybackSession): void {
    // New session goes at index 0
    // Shift all existing items down by 1
    const newCache = new Map<number, PlaybackSession>();
    newCache.set(0, session);
    
    this._cachedData.forEach((value, key) => {
      newCache.set(key + 1, value);
    });
    
    this._cachedData = newCache;
    
    // Update ranges
    this._fetchedRanges = this._fetchedRanges.map(range => ({
      start: range.start + 1,
      end: range.end + 1
    }));
    
    // Add the new first item as a fetched range
    this._fetchedRanges.unshift({ start: 0, end: 1 });
    this._mergeFetchedRanges();
    
    // Increment total length
    this._totalLength++;
    
    this._updateDataStream();
  }

  updateSession(sessionId: string, updatedSession: PlaybackSession): void {
    // Find and update the session in cache
    for (const [index, session] of this._cachedData.entries()) {
      if (session.id === sessionId) {
        this._cachedData.set(index, updatedSession);
        this._updateDataStream();
        break;
      }
    }
  }
}
