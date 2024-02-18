import {FileExplorerNodeType} from "../../../../main-content/file-explorer/enums/file-explorer-node-type";

export interface NodeListItemInterface {
  name: string;
  disabled: boolean;
  type: FileExplorerNodeType;
  mdIcon: string;
}
