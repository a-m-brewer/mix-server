import {FileExplorerNodeType} from "../../../../../main-content/file-explorer/enums/file-explorer-node-type";

export interface NodeListItemChangedEvent {
  id: string;
  nodeType: FileExplorerNodeType
}
