import {FileExplorerFolderNode} from "./file-explorer-folder-node";
import {FileExplorerNode} from "./file-explorer-node";

export class FileExplorerPopulatedFolderNode {
  constructor(public parent: FileExplorerFolderNode,
              public children: FileExplorerNode[]) {
  }
}
