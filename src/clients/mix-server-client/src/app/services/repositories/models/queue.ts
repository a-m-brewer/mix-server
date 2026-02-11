import {QueueItem} from "./queue-item";

export class Queue {
  constructor(public currentQueuePosition: string | null | undefined,
              public previousQueuePosition: string | null | undefined,
              public nextQueuePosition: string | null | undefined,
              public items: QueueItem[]) {
  }
}
