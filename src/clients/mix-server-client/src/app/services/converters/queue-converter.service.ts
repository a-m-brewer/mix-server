import { Injectable } from '@angular/core';
import {QueueSnapshotDto, QueueSnapshotItemDto} from "../../generated-clients/mix-server-clients";
import {Queue} from "../repositories/models/queue";
import {FileExplorerNodeConverterService} from "./file-explorer-node-converter.service";
import {QueueItem} from "../repositories/models/queue-item";

@Injectable({
  providedIn: 'root'
})
export class QueueConverterService {

  constructor(private _nodeConverter: FileExplorerNodeConverterService) {
  }

  public fromDto(dto: QueueSnapshotDto): Queue {
    return new Queue(dto.currentQueuePosition, dto.items.map(item => this.fromQueueItemDto(dto.currentQueuePosition, item)));
  }

  public fromQueueItemDto(currentQueuePosition: string | null | undefined, dto: QueueSnapshotItemDto): QueueItem {
    return new QueueItem(dto.id, dto.type, this._nodeConverter.fromFileExplorerFileNode(dto.file),  dto.id === currentQueuePosition);
  }
}
