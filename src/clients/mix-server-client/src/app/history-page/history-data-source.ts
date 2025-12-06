import { DataSource, CollectionViewer } from '@angular/cdk/collections';
import { Observable, BehaviorSubject, Subject, Subscription } from 'rxjs';
import { PlaybackSession } from '../services/repositories/models/playback-session';

export class HistoryDataSource extends DataSource<PlaybackSession> {
  private _length$ = new BehaviorSubject<number>(0);
  private _cachedData = new Map<number, PlaybackSession>();
  private _fetchedRanges: { start: number; end: number }[] = [];
  private _dataStream$ = new BehaviorSubject<PlaybackSession[]>([]);
  private _subscription = new Subscription();

  constructor(
    private _fetchRange: (start: number, end: number) => Promise<PlaybackSession[]>,
    private _getInitialLength: () => Promise<number>
  ) {
    super();
  }

  connect(collectionViewer: CollectionViewer): Observable<PlaybackSession[]> {
    this._subscription.add(
      collectionViewer.viewChange.subscribe(range => {
        const startIndex = range.start;
        const endIndex = range.end;
        
        // Check if we need to fetch this range
        if (!this._isRangeFetched(startIndex, endIndex)) {
          this._fetchRangeData(startIndex, endIndex).then();
        }
      })
    );

    return this._dataStream$.asObservable();
  }

  disconnect(): void {
    this._subscription.unsubscribe();
  }

  get length$(): Observable<number> {
    return this._length$.asObservable();
  }

  async initialize(): Promise<void> {
    const initialLength = await this._getInitialLength();
    this._length$.next(initialLength);
    
    // Load initial range
    await this._fetchRangeData(0, Math.min(50, initialLength));
  }

  async reset(): Promise<void> {
    this._cachedData.clear();
    this._fetchedRanges = [];
    this._dataStream$.next([]);
    await this.initialize();
  }

  private _isRangeFetched(start: number, end: number): boolean {
    return this._fetchedRanges.some(
      range => range.start <= start && range.end >= end
    );
  }

  private async _fetchRangeData(start: number, end: number): Promise<void> {
    try {
      const data = await this._fetchRange(start, end);
      
      // Cache the fetched data
      data.forEach((session, index) => {
        this._cachedData.set(start + index, session);
      });

      // Record the fetched range
      this._fetchedRanges.push({ start, end });
      this._mergeFetchedRanges();

      // Update the data stream
      this._updateDataStream();
    } catch (error) {
      console.error('Error fetching range data:', error);
    }
  }

  private _mergeFetchedRanges(): void {
    // Sort ranges by start
    this._fetchedRanges.sort((a, b) => a.start - b.start);

    // Merge overlapping ranges
    const merged: { start: number; end: number }[] = [];
    for (const range of this._fetchedRanges) {
      if (merged.length === 0) {
        merged.push(range);
        continue;
      }

      const last = merged[merged.length - 1];
      if (range.start <= last.end) {
        // Merge with previous range
        last.end = Math.max(last.end, range.end);
      } else {
        merged.push(range);
      }
    }

    this._fetchedRanges = merged;
  }

  private _updateDataStream(): void {
    const data: PlaybackSession[] = [];
    const keys = Array.from(this._cachedData.keys()).sort((a, b) => a - b);
    
    for (const key of keys) {
      const session = this._cachedData.get(key);
      if (session) {
        data.push(session);
      }
    }

    this._dataStream$.next(data);
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
    this._length$.next(this._length$.value + 1);
    
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
