import { Injectable } from '@angular/core';
import {BehaviorSubject, distinctUntilChanged, Observable} from "rxjs";

@Injectable({
  providedIn: 'root'
})
export class VisibilityRepositoryService {
  private _visibilitySubject$ = new BehaviorSubject<DocumentVisibilityState>("visible");

  constructor() { }

  public get visibility(): DocumentVisibilityState {
    return this._visibilitySubject$.getValue();
  }

  public get visibility$(): Observable<DocumentVisibilityState> {
    return this._visibilitySubject$
      .pipe(distinctUntilChanged());
  }

  public set visibility(value: DocumentVisibilityState) {
    this._visibilitySubject$.next(value);
  }
}
