import {FileExplorerNodeType} from "../enums/file-explorer-node-type";
import {FileExplorerFolderNode} from "./file-explorer-folder-node";
import {FileExplorerNodeStateInterface} from "./file-explorer-node-state";
import {NodeListItemInterface} from "../../../components/nodes/node-list/node-list-item/node-list-item.interface";

export interface FileExplorerNode extends NodeListItemInterface {
  absolutePath: string;
  exists: boolean;
  creationTimeUtc: Date;
  parent: FileExplorerFolderNode | null | undefined;
  isEqual(node: FileExplorerNode | null | undefined): boolean;
  copy(): FileExplorerNode;
}
