import {FileExplorerNodeType} from "../../../../../main-content/file-explorer/enums/file-explorer-node-type";
import {FileExplorerNodeState} from "../../../../../main-content/file-explorer/enums/file-explorer-node-state.enum";

export interface NodeListItem {
  disabled: boolean;
  editing: boolean;
  selected: boolean;
  isCurrentSession: boolean;
  type: FileExplorerNodeType;
  name: string;
  state: FileExplorerNodeState
  mdIcon: string
}
