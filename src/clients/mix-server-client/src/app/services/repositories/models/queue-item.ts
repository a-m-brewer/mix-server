import {FileExplorerFileNode} from "../../../main-content/file-explorer/models/file-explorer-file-node";
import { FileExplorerNodeType } from "src/app/main-content/file-explorer/enums/file-explorer-node-type";
import {QueueItemType} from "../../../generated-clients/mix-server-clients";
import {PagedDataItem} from "../../data-sources/paged-data";

export class QueueItem implements PagedDataItem<QueueItem> {
  constructor(public id: string,
              public itemType: QueueItemType,
              public rank: string,
              public isCurrentPosition: boolean,
              initialFile: FileExplorerFileNode) {
    this.file = initialFile;
  }

  public file: FileExplorerFileNode;

  public get name(): string {
    return this.file.path.fileName;
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

  public copy(): QueueItem {
    return new QueueItem(this.id, this.itemType, this.rank, this.isCurrentPosition, this.file)
  }
}
