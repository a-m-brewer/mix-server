import {FileExplorerFolderNode} from "./file-explorer-folder-node";
import {FileExplorerNodeType} from "../enums/file-explorer-node-type";
import {NodePath} from "./node-path";
import {PagedDataItem} from "../../../services/data-sources/paged-data";

export interface FileExplorerNode extends PagedDataItem<FileExplorerNode> {
  path: NodePath;
  type: FileExplorerNodeType;
  creationTimeUtc: Date;
  exists: boolean;
  disabled: boolean;
  mdIcon: string;
  parent: FileExplorerFolderNode | null | undefined;
  isEqual(node: FileExplorerNode | null | undefined): boolean;
}
