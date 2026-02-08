import {DataSource, CollectionViewer} from '@angular/cdk/collections';
import {Observable, BehaviorSubject, Subscription} from 'rxjs';
import {debounceTime} from 'rxjs/operators';
import {FileExplorerNode} from './models/file-explorer-node';
import {FileExplorerFolderNode} from './models/file-explorer-folder-node';
import {FolderSort} from './models/folder-sort';
import {NodePathHeader} from './models/node-path';

export interface FileExplorerFetchResult {
  children: FileExplorerNode[];
  totalCount: number;
  node: FileExplorerFolderNode;
  sort: FolderSort;
}

export class FileExplorerDataSource extends DataSource<FileExplorerNode | undefined> {
  private _cachedData = new Map<number, FileExplorerNode>();
  private _fetchedRanges: { start: number; end: number }[] = [];
  private _dataStream$ = new BehaviorSubject<(FileExplorerNode | undefined)[]>([]);
  private _subscription = new Subscription();
  private _totalCount = 0;
  private _hasMore = true;
  private _isFetching = false;
  private _fetchingRanges = new Set<string>();
  private _currentPath: NodePathHeader | null = null;

  private _folderNode$ = new BehaviorSubject<FileExplorerFolderNode>(FileExplorerFolderNode.Default);
  private _sort$ = new BehaviorSubject<FolderSort>(FolderSort.Default);

  constructor(
    private _fetchRange: (rootPath: string, relativePath: string, start: number, end: number)
      => Promise<FileExplorerFetchResult>
  ) {
    super();
  }

  get folderNode$(): Observable<FileExplorerFolderNode> {
    return this._folderNode$.asObservable();
  }

  get sort$(): Observable<FolderSort> {
    return this._sort$.asObservable();
  }

  get totalCount(): number {
    return this._totalCount;
  }

  get currentData(): (FileExplorerNode | undefined)[] {
    return this._dataStream$.value;
  }

  connect(collectionViewer: CollectionViewer): Observable<(FileExplorerNode | undefined)[]> {
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

  async loadFolder(path: NodePathHeader): Promise<void> {
    this._currentPath = path;
    this._clearCache();
    await this._fetchInitial();
  }

  async reset(): Promise<void> {
    if (!this._currentPath) {
      return;
    }

    this._clearCache();
    await this._fetchInitial();
  }

  updateNodeMetadata(key: string, updater: (node: FileExplorerNode) => void): void {
    let updated = false;
    for (const [, node] of this._cachedData) {
      if (node.path.key === key) {
        updater(node);
        updated = true;
        break;
      }
    }

    if (updated) {
      this._updateDataStream();
    }
  }

  updateAllNodes(updater: (node: FileExplorerNode) => void): void {
    for (const [, node] of this._cachedData) {
      updater(node);
    }
    if (this._cachedData.size > 0) {
      this._updateDataStream();
    }
  }

  private _clearCache(): void {
    this._cachedData.clear();
    this._fetchedRanges = [];
    this._totalCount = 0;
    this._hasMore = true;
    this._isFetching = false;
    this._fetchingRanges.clear();
  }

  private async _fetchInitial(): Promise<void> {
    if (!this._currentPath) {
      this._dataStream$.next([]);
      return;
    }

    try {
      const result = await this._fetchRange(
        this._currentPath.rootPath,
        this._currentPath.relativePath,
        0,
        50
      );

      // Check if folder changed during fetch
      if (!this._currentPath || this._currentPath.key !== (result.node.path?.key ?? '')) {
        // The path key from the API might differ from what we navigated to (e.g. root resolves differently)
        // Accept the result if _currentPath hasn't been cleared
      }

      this._folderNode$.next(result.node);
      this._sort$.next(result.sort);
      this._totalCount = result.totalCount;

      if (result.children.length === 0) {
        this._hasMore = false;
        this._dataStream$.next([]);
        return;
      }

      result.children.forEach((node, index) => {
        this._cachedData.set(index, node);
      });

      this._fetchedRanges.push({start: 0, end: result.children.length});

      this._hasMore = result.children.length < this._totalCount;

      this._updateDataStream();
    } catch (error) {
      console.error('Error loading folder:', error);
      this._dataStream$.next([]);
    }
  }

  private _isRangeFetched(start: number, end: number): boolean {
    return this._fetchedRanges.some(
      range => range.start <= start && range.end >= end
    );
  }

  private async _fetchRangeData(start: number, end: number): Promise<void> {
    if (this._isFetching || !this._currentPath) {
      return;
    }

    const rangeKey = `${start}-${end}`;

    if (this._fetchingRanges.has(rangeKey)) {
      return;
    }

    this._isFetching = true;
    this._fetchingRanges.add(rangeKey);

    const fetchPath = this._currentPath;

    try {
      const fetchStart = start;
      const fetchEnd = Math.min(end, start + 50);

      const result = await this._fetchRange(
        fetchPath.rootPath,
        fetchPath.relativePath,
        fetchStart,
        fetchEnd
      );

      // Check if folder changed during fetch
      if (!this._currentPath || !this._currentPath.isEqual(fetchPath)) {
        return;
      }

      if (result.children.length === 0) {
        this._hasMore = false;
        return;
      }

      // Update totalCount in case it changed
      this._totalCount = result.totalCount;

      result.children.forEach((node, index) => {
        this._cachedData.set(fetchStart + index, node);
      });

      this._fetchedRanges.push({start: fetchStart, end: fetchStart + result.children.length});
      this._mergeFetchedRanges();

      if (fetchStart + result.children.length >= this._totalCount) {
        this._hasMore = false;
      }

      this._updateDataStream();
    } catch (error) {
      console.error('Error fetching range data:', error);
    } finally {
      this._fetchingRanges.delete(rangeKey);
      this._isFetching = false;
    }
  }

  private _mergeFetchedRanges(): void {
    this._fetchedRanges.sort((a, b) => a.start - b.start);

    const merged: { start: number; end: number }[] = [];
    for (const range of this._fetchedRanges) {
      if (merged.length === 0) {
        merged.push(range);
        continue;
      }

      const last = merged[merged.length - 1];
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

    const dataArray: (FileExplorerNode | undefined)[] = new Array(length);

    this._cachedData.forEach((node, index) => {
      if (index < length) {
        dataArray[index] = node;
      }
    });

    this._dataStream$.next(dataArray);
  }
}
