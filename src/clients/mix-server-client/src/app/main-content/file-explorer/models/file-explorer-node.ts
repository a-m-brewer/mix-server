import {FileExplorerNodeType} from "../enums/file-explorer-node-type";
import {FileExplorerFolderInfoNode} from "./file-explorer-folder-info-node";

export interface FileExplorerNode {
  name: string;
  absolutePath: string;
  type: FileExplorerNodeType;
  exists: boolean;
  creationTimeUtc: Date;
  disabled: boolean,
  parent: FileExplorerFolderInfoNode | null | undefined;
  isEqual(node: FileExplorerNode | null | undefined): boolean;
  copy(): FileExplorerNode;
}
