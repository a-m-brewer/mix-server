import { Injectable } from '@angular/core';
import {BehaviorSubject, Observable} from "rxjs";

@Injectable({
  providedIn: 'root'
})
export class InitializationRepositoryService {
  private initializedBehaviourSubject$ = new BehaviorSubject<boolean>(false);

  constructor() { }

  public get initialized$(): Observable<boolean> {
    return this.initializedBehaviourSubject$.asObservable();
  }

  public get initialized(): boolean {
    return this.initializedBehaviourSubject$.getValue();
  }
  public set initialized(value: boolean) {
    this.initializedBehaviourSubject$.next(value);
  }
}
