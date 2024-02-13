import {FileExplorerNode} from "../../../main-content/file-explorer/models/file-explorer-node";

export class NodeUpdatedEvent {
    constructor(public readonly node: FileExplorerNode,
                public readonly oldAbsolutePath: string)
    {
    }
}
