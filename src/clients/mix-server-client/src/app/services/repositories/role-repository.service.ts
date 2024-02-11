import { Injectable } from '@angular/core';
import {BehaviorSubject, map, Observable} from "rxjs";

@Injectable({
  providedIn: 'root'
})
export class RoleRepositoryService {
  private _rolesBehaviourSubject$ = new BehaviorSubject<Array<string>>([]);

  constructor() { }

  public set roles(roles: Array<string>) {
    this._rolesBehaviourSubject$.next(roles);
  }

  public inRole$(name: string): Observable<boolean> {
    return this._rolesBehaviourSubject$
      .pipe(map(m => m.includes(name)));
  }
}
