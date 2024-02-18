import { Injectable } from '@angular/core';
import {BehaviorSubject, Observable} from "rxjs";
import {LoadingNodeStatus} from "./models/loading-node-status";

@Injectable({
  providedIn: 'root'
})
export class LoadingRepositoryService {
  private _loadingCount = 0;
  private _status$ = new BehaviorSubject<LoadingNodeStatus>({loading: false, loadingIds: []});

  constructor() { }

  public get status(): LoadingNodeStatus {
    return this._status$.value;
  }

  public status$(): Observable<LoadingNodeStatus> {
    return this._status$.asObservable();
  }

  public startLoadingId(id: string | null | undefined): void {
    this.nextLoading(true, id);
  }

  public startLoading(): void {
    this.nextLoading(true)
  }

  public stopLoadingId(id: string | null | undefined): void {
    this.nextLoading(false, id);
  }

  public stopLoading(): void {
    this.nextLoading(false);
  }

  public nextLoading(loading: boolean, id?: string | null): void {
    const change = loading ? 1 : -1;
    const nextCount = Math.max(0, this._loadingCount + change);

    const nextLoading = 0 < nextCount;

    let nextLoadingIds = [...this._status$.value.loadingIds];
    if (id) {
      if (loading) {
        nextLoadingIds.push(id);
      } else {
        nextLoadingIds = nextLoadingIds.filter(x => x !== id);
      }
    }
    nextLoadingIds = [...new Set(nextLoadingIds)];

    this._loadingCount = nextCount;
    this._status$.next({loading: nextLoading, loadingIds: nextLoadingIds});
  }
}
