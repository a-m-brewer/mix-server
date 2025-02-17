import {FileExplorerFileNode} from "../../../main-content/file-explorer/models/file-explorer-file-node";
import {QueueSnapshotItemType} from "../../../generated-clients/mix-server-clients";
import { FileExplorerNodeType } from "src/app/main-content/file-explorer/enums/file-explorer-node-type";
import {Observable, Subject, takeUntil} from "rxjs";

export class QueueItem {
  constructor(public id: string,
              public itemType: QueueSnapshotItemType,
              initialFile: FileExplorerFileNode,
              public file$: Observable<FileExplorerFileNode>,
              public isCurrentQueuePosition: boolean,
              unsubscribe$: Subject<void>) {
    this.file = initialFile;
    file$.pipe(takeUntil(unsubscribe$)).subscribe(file => {
      this.file = file;
    });
  }

  public file: FileExplorerFileNode;

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
