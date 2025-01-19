import {FileExplorerNode} from "../../../main-content/file-explorer/models/file-explorer-node";

export class NodeAddedEvent {
    constructor(public readonly node: FileExplorerNode,
                public readonly index: number)
    {
    }
}
