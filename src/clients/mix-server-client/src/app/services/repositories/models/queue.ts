import {QueueItem} from "./queue-item";
import {Observable, Subject, Subscription} from "rxjs";

export class Queue {
  constructor(public currentQueuePosition: string | null | undefined,
              public items: QueueItem[]) {
  }
}
