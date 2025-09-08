import {PagedData, PagedDataPage} from "../../data-sources/paged-data";
import {FileExplorerNode} from "../../../main-content/file-explorer/models/file-explorer-node";
import {QueueItem} from "./queue-item";

export class PagedQueue extends PagedData<QueueItem> {
  constructor(pages: PagedDataPage<QueueItem>[]) {
    super(pages);
  }

  public static get Default(): PagedQueue {
    return new PagedQueue([]);
  }

  public copy(): PagedQueue {
    return new PagedQueue(
      Object.values(this.pages).map(page => page.copy())
    );
  }
}
