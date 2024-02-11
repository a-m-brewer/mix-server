import { Injectable } from '@angular/core';
import {BehaviorSubject, Observable} from "rxjs";

@Injectable({
  providedIn: 'root'
})
export class ScrollContainerRepositoryService {
  private _scrollTop$ = new BehaviorSubject<number>(0);

  constructor() { }

  public get scrollTop$(): Observable<number> {
    return this._scrollTop$.asObservable();
  }

  onScrollTop(scrollTop: number | undefined) {
    this._scrollTop$.next(scrollTop ?? 0);
  }
}
