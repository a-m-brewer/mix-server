import {QueuePosition} from "../../repositories/models/QueuePosition";
import {QueueItem} from "../../repositories/models/queue-item";

export interface QueueItemsAddedEvent {
  position: QueuePosition;
  added: QueueItem[];
}
