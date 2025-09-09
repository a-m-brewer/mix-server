import { Injectable } from '@angular/core';
import {
  QueuePageDto,
  QueuePositionDto,
  QueueRangeDto,
  QueueSnapshotItemDto
} from "../../generated-clients/mix-server-clients";
import {FileExplorerNodeConverterService} from "./file-explorer-node-converter.service";
import {QueueItem} from "../repositories/models/queue-item";
import {QueuePosition} from "../repositories/models/QueuePosition";
import {PagedDataPage} from "../data-sources/paged-data";

@Injectable({
  providedIn: 'root'
})
export class QueueConverterService {

  constructor(private _nodeConverter: FileExplorerNodeConverterService) {
  }

  public toQueueItemList(dto: QueueRangeDto): QueueItem[] {
    return dto.items.map(item => this.toQueueItem(item))
  }

  public toQueuePosition(dto: QueuePositionDto): QueuePosition {
    return new QueuePosition(
      dto.currentQueuePosition ? this.toQueueItem(dto.currentQueuePosition) : null,
      dto.previousQueuePosition ? this.toQueueItem(dto.previousQueuePosition) : null,
      dto.nextQueuePosition ? this.toQueueItem(dto.nextQueuePosition) : null
    )
  }

  public toQueueItem(dto: QueueSnapshotItemDto): QueueItem {
    return new QueueItem(dto.id, dto.type, dto.rank, dto.isCurrentPosition, this._nodeConverter.fromFileExplorerFileNode(dto.file));
  }
}
