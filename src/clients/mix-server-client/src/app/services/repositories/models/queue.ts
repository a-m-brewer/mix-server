import {QueueItem} from "./queue-item";
import {Observable, Subject, Subscription} from "rxjs";

export class Queue {
  constructor(public currentQueuePosition: string | null | undefined,
              public previousQueuePosition: string | null | undefined,
              public nextQueuePosition: string | null | undefined,
              public items: QueueItem[],
              private _unsubscribe$?: Subject<void>) {
  }

  public destroy(): void {
    if (this._unsubscribe$) {
      this._unsubscribe$.next();
      this._unsubscribe$.complete();
    }
  }
}
