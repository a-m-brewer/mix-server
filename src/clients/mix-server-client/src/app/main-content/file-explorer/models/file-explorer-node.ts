import {FileExplorerFolderNode} from "./file-explorer-folder-node";
import {FileExplorerNodeType} from "../enums/file-explorer-node-type";

export interface FileExplorerNode {
  name: string;
  absolutePath: string;
  type: FileExplorerNodeType;
  creationTimeUtc: Date;
  exists: boolean;
  disabled: boolean;
  mdIcon: string;
  parent: FileExplorerFolderNode | null | undefined;
  isEqual(node: FileExplorerNode | null | undefined): boolean;
  copy(): FileExplorerNode;
}
