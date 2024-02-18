import {FileExplorerFileNode} from "../../../main-content/file-explorer/models/file-explorer-file-node";
import {QueueSnapshotItemType} from "../../../generated-clients/mix-server-clients";
import {NodeListItemInterface} from "../../../components/nodes/node-list/node-list-item/node-list-item.interface";
import { FileExplorerNodeType } from "src/app/main-content/file-explorer/enums/file-explorer-node-type";

export class QueueItem implements NodeListItemInterface {
  constructor(public id: string,
              public itemType: QueueSnapshotItemType,
              public file: FileExplorerFileNode,
              public isCurrentQueuePosition: boolean) {
  }

  public get editable(): boolean {
    return this.itemType === QueueSnapshotItemType.User;
  }

  public get name(): string {
    return this.file.name;
  }

  public get mdIcon(): string {
    return this.file.mdIcon;
  }

  public get type(): FileExplorerNodeType {
    return this.file.type;
  }

  public get disabled(): boolean {
    return this.file.disabled;
  }
}
