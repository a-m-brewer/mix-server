import {FileExplorerFolderNode} from "./file-explorer-folder-node";
import {FileExplorerNode} from "./file-explorer-node";
import {FolderSort} from "./folder-sort";

export class FileExplorerFolderPage {
  constructor(public pageIndex: number,
              public children: FileExplorerNode[]) {
  }

  public copy(): FileExplorerFolderPage {
    return new FileExplorerFolderPage(this.pageIndex, this.children.map(child => child.copy()));
  }
}

export class PagedFileExplorerFolder {
  public flatChildren: FileExplorerNode[] = [];

  constructor(public node: FileExplorerFolderNode,
              public pages: FileExplorerFolderPage[],
              public sort: FolderSort) {
    this.flatChildren = PagedFileExplorerFolder.getFlatChildren(pages);
  }

  public static get Default(): PagedFileExplorerFolder {
    return new PagedFileExplorerFolder(FileExplorerFolderNode.Default, [], FolderSort.Default);
  }

  public copy(): PagedFileExplorerFolder {
    return new PagedFileExplorerFolder(
      this.node.copy(),
      this.pages.map(page => page.copy()),
      this.sort.copy()
    );
  }

  private static getFlatChildren(pages: FileExplorerFolderPage[]): FileExplorerNode[] {
    return pages.sort((a, b) => a.pageIndex - b.pageIndex).flatMap(page => page.children);
  }

  refreshFlatChildren() {
    this.flatChildren = PagedFileExplorerFolder.getFlatChildren(this.pages);
  }
}
