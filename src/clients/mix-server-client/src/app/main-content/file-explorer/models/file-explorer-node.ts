import {FileExplorerNodeType} from "../enums/file-explorer-node-type";
import {FileExplorerFolderNode} from "./file-explorer-folder-node";
import {FileExplorerNodeStateInterface} from "./file-explorer-node-state";

export interface FileExplorerNode {
  name: string;
  absolutePath: string;
  type: FileExplorerNodeType;
  exists: boolean;
  creationTimeUtc: Date;
  disabled: boolean,
  parent: FileExplorerFolderNode | null | undefined;
  state: FileExplorerNodeStateInterface;
  mdIcon: string;
  isEqual(node: FileExplorerNode | null | undefined): boolean;
  copy(): FileExplorerNode;
}
