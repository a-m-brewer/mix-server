import {FileExplorerFileNode} from "../../../main-content/file-explorer/models/file-explorer-file-node";
import {QueueSnapshotItemType} from "../../../generated-clients/mix-server-clients";

export class QueueItem {
  constructor(public id: string,
              public itemType: QueueSnapshotItemType,
              public file: FileExplorerFileNode,
              public isCurrentQueuePosition: boolean) {
  }
}
