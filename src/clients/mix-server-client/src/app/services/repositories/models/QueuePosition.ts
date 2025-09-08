import {QueueItem} from "./queue-item";

export class QueuePosition {
  constructor(public current: QueueItem | null | undefined,
              public previous: QueueItem | null | undefined,
              public next: QueueItem | null | undefined) {
  }

  public static get Default(): QueuePosition {
    return new QueuePosition(null, null, null);
  }
}
