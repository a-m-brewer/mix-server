import { Injectable } from '@angular/core';
import {QueueSnapshotDto, QueueSnapshotItemDto} from "../../generated-clients/mix-server-clients";
import {Queue} from "../repositories/models/queue";
import {FileExplorerNodeConverterService} from "./file-explorer-node-converter.service";
import {QueueItem} from "../repositories/models/queue-item";
import {Subject} from "rxjs";
import {NodeCacheService} from "../nodes/node-cache.service";

@Injectable({
  providedIn: 'root'
})
export class QueueConverterService {

  constructor(private _nodeCache: NodeCacheService,
              private _nodeConverter: FileExplorerNodeConverterService) {
  }

  public fromDto(dto: QueueSnapshotDto): Queue {
    const unsubscribe$ = new Subject<void>();
    return new Queue(
      dto.currentQueuePosition,
      dto.previousQueuePosition,
      dto.nextQueuePosition,
      dto.items.map(item => this.fromQueueItemDto(dto.currentQueuePosition, item, unsubscribe$)),
      unsubscribe$
    );
  }

  public fromQueueItemDto(currentQueuePosition: string | null | undefined, dto: QueueSnapshotItemDto, unsubscribe$: Subject<void>): QueueItem {
    const initialFile = this._nodeConverter.fromFileExplorerFileNode(dto.file);
    return new QueueItem(
      dto.id,
      dto.type,
      initialFile,
      this._nodeCache.getFileByNode$(initialFile),
      dto.id === currentQueuePosition,
      unsubscribe$
    );
  }
}
