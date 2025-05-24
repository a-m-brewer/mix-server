import {FileExplorerFolderNode} from "./file-explorer-folder-node";
import {FileExplorerNodeType} from "../enums/file-explorer-node-type";
import {NodePath} from "./node-path";

export interface FileExplorerNode {
  path: NodePath;
  type: FileExplorerNodeType;
  creationTimeUtc: Date;
  exists: boolean;
  disabled: boolean;
  mdIcon: string;
  parent: FileExplorerFolderNode | null | undefined;
  isEqual(node: FileExplorerNode | null | undefined): boolean;
  copy(): FileExplorerNode;
}
