import {QueueItem} from "./queue-item";
import {Observable, Subject, Subscription} from "rxjs";

export class Queue {
  private _selectedSubject$ = new Subject<QueueItem>();

  private _subscriptions: Subscription[] = [];

  constructor(public currentQueuePosition: string | null | undefined,
              public items: QueueItem[]) {
    this.subscribeQueueSubscriptions();
  }

  public get itemSelected$(): Observable<QueueItem> {
    return this._selectedSubject$.asObservable();
  }

  private subscribeQueueSubscriptions(): void {
    this._subscriptions = this.items.map(i => i.selected$.subscribe(s => this.handleSelectionChange(i)));
  }

  public unsubscribeQueueSubscriptions(): void {
    this._subscriptions.forEach(s => s.unsubscribe());
    this._subscriptions = [];
  }

  private handleSelectionChange(item: QueueItem): void {
    this._selectedSubject$.next(item);
  }
}
