import {QueueItem} from "./queue-item";
import {Observable, Subject, Subscription} from "rxjs";

export class Queue {
  constructor(public currentQueuePosition: string | null | undefined,
              public items: QueueItem[]) {
  }

  public findNextValidOffset(offset: number, validationFunc: (queueItem: QueueItem) => boolean): number | null {
    const currentIndex = this.items.findIndex(f => f.id === this.currentQueuePosition);
    let offsetIndex = currentIndex + offset;

    while (offsetIndex >= 0 && offsetIndex < this.items.length) {
      const item = this.items[offsetIndex];
      if (!item.file.serverPlaybackDisabled && validationFunc(item)) {
        return offsetIndex - currentIndex;
      }

      const increment = offset < 0 ? -1 : 1;
      offsetIndex += increment;
    }

    return null;
  }
}
