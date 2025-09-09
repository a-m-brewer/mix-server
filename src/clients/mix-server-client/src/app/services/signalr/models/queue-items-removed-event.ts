import {QueuePosition} from "../../repositories/models/QueuePosition";

export interface QueueItemsRemovedEvent {
    position: QueuePosition;
    removed: string[];
}
