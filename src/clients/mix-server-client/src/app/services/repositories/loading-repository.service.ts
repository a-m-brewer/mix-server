import {Injectable} from '@angular/core';
import {BehaviorSubject, Observable} from "rxjs";
import {LoadingAction, LoadingNodeStatus, LoadingNodeStatusImpl} from "./models/loading-node-status";
import {cloneDeep} from "lodash";
import {environment} from "../../../environments/environment";
import {EnvironmentType} from "../../../environments/environment-type.enum";

@Injectable({
  providedIn: 'root'
})
export class LoadingRepositoryService {
  private _loadingCount = 0;
  private _status$ = new BehaviorSubject<LoadingNodeStatus>(LoadingNodeStatusImpl.new);

  constructor() { }

  public get status(): LoadingNodeStatus {
    return this._status$.value;
  }

  public status$(): Observable<LoadingNodeStatus> {
    return this._status$.asObservable();
  }

  public startLoadingAction(action: LoadingAction): void {
    this.nextLoading(true, action);
  }

  public startLoading(id: string): void {
    this.nextLoading(true, id);
  }

  public startLoadingIds(ids: Array<string>): void {
    ids.forEach(id => this.startLoading(id));
  }

  public stopLoadingAction(action: LoadingAction): void {
    this.nextLoading(false, action);
  }

  public stopLoading(id: string): void {
    this.nextLoading(false, id);
  }

  public stopLoadingItems(ids: Array<string>): void {
    ids.forEach(id => this.stopLoading(id));
  }

  public nextLoading(loading: boolean, id: string): void {
    const loadingIds = cloneDeep(this._status$.value.loadingIds);

    const change = loading ? 1 : -1;
    const nextCount = Math.max(0, this._loadingCount + change);

    const nextLoading = 0 < nextCount;

    if (id) {
      const nextValue = Math.max(0, (loadingIds[id] || 0) + change);

      if (nextValue === 0 && loadingIds[id]) {
        delete loadingIds[id];
      } else {
        loadingIds[id] = nextValue;
      }
    }

    this._loadingCount = nextCount;

    if (environment.type === EnvironmentType.Development) {
      console.log(this._loadingCount, loadingIds);
    }

    this._status$.next(new LoadingNodeStatusImpl(nextLoading, loadingIds));
  }

  isLoading(loadingKey: string) {
    return this._status$.value.loadingIds[loadingKey] > 0;
  }
}
