import { Injectable } from '@angular/core';
import {BehaviorSubject, Observable} from "rxjs";

@Injectable({
  providedIn: 'root'
})
export class LoadingRepositoryService {
  private _loadingCount: number = 0;
  private _loadingBehaviour = new BehaviorSubject<boolean>(false);

  constructor() { }

  public loading$(): Observable<boolean> {
    return this._loadingBehaviour.asObservable();
  }

  public set loading(value: boolean) {
    const change = value ? 1 : -1;
    const nextCount = Math.max(0, this._loadingCount + change);
    const nextLoading = 0 < nextCount;

    this._loadingCount = nextCount;
    console.log('Loading count: ' + this._loadingCount);
    if (this._loadingBehaviour.getValue() !== nextLoading) {
      this._loadingBehaviour.next(nextLoading);
    }
  }
}
