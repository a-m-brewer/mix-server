import {FileExplorerFolderNode} from "./file-explorer-folder-node";
import {FileExplorerNode} from "./file-explorer-node";
import {FolderSort} from "./folder-sort";
import {PagedData, PagedDataPage} from "../../../services/data-sources/paged-data";

export class PagedFileExplorerFolder extends PagedData<FileExplorerNode> {

  constructor(public node: FileExplorerFolderNode,
              pages: PagedDataPage<FileExplorerNode>[],
              public sort: FolderSort) {
    super(pages);
  }

  public static get Default(): PagedFileExplorerFolder {
    return new PagedFileExplorerFolder(FileExplorerFolderNode.Default, [], FolderSort.Default);
  }

  public copy(): PagedFileExplorerFolder {
    return new PagedFileExplorerFolder(
      this.node.copy(),
      Object.values(this.pages).map(page => page.copy()),
      this.sort.copy()
    );
  }

}
