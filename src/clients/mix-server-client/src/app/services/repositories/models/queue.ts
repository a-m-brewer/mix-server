import {QueueItem} from "./queue-item";
import {Observable, Subject, Subscription} from "rxjs";

export class Queue {
  constructor(public currentQueuePosition: string | null | undefined,
              public items: QueueItem[],
              private _unsubscribe$?: Subject<void>) {
  }

  public destroy(): void {
    if (this._unsubscribe$) {
      this._unsubscribe$.next();
      this._unsubscribe$.complete();
    }
  }

  public findNextValidOffset(offset: number): number | null {
    const currentIndex = this.items.findIndex(f => f.id === this.currentQueuePosition);
    let offsetIndex = currentIndex + offset;

    while (offsetIndex >= 0 && offsetIndex < this.items.length) {
      const item = this.items[offsetIndex];
      if (!item.file.disabled) {
        return offsetIndex - currentIndex;
      }

      const increment = offset < 0 ? -1 : 1;
      offsetIndex += increment;
    }

    return null;
  }

  public hasValidOffset(offset: number): boolean {
    return this.findNextValidOffset(offset) !== null;
  }
}
