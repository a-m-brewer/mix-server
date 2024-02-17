import {FileExplorerNodeType} from "../enums/file-explorer-node-type";

export interface FileExplorerNode {
  name: string;
  absolutePath: string;
  type: FileExplorerNodeType;
  exists: boolean;
  creationTimeUtc: Date;
}
