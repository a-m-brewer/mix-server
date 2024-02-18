import { Injectable } from '@angular/core';
import {BehaviorSubject, Observable} from "rxjs";
import {LoadingNodeStatus} from "./models/loading-node-status";

@Injectable({
  providedIn: 'root'
})
export class LoadingRepositoryService {
  private _status$ = new BehaviorSubject<LoadingNodeStatus>({loading: false});

  constructor() { }

  public get status(): LoadingNodeStatus {
    return this._status$.value;
  }

  public status$(): Observable<LoadingNodeStatus> {
    return this._status$.asObservable();
  }

  public startLoadingItem(id?: string | null): void {
    this._status$.next({loading: true, id});
  }

  public startLoading(): void {
    this._status$.next({loading: true});
  }

  public stopLoading(): void {
    this._status$.next({loading: false});
  }
}
