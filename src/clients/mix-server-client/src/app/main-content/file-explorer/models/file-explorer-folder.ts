import {FileExplorerFolderNode} from "./file-explorer-folder-node";
import {FileExplorerNode} from "./file-explorer-node";
import {FolderSort} from "./folder-sort";

export class FileExplorerFolder {
  constructor(public node: FileExplorerFolderNode,
              public children: FileExplorerNode[],
              public sort: FolderSort) {
  }

  public static get Default(): FileExplorerFolder {
    return new FileExplorerFolder(FileExplorerFolderNode.Default, [], FolderSort.Default);
  }
}
