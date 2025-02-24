import {FileExplorerNode} from "../../../main-content/file-explorer/models/file-explorer-node";

export class NodeUpdatedEvent {
    constructor(public readonly node: FileExplorerNode,
                public readonly index: number,
                public readonly oldAbsolutePath?: string)
    {
    }
}
