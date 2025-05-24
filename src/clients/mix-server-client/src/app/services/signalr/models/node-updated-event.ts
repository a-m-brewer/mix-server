import {FileExplorerNode} from "../../../main-content/file-explorer/models/file-explorer-node";
import {NodePath} from "../../../main-content/file-explorer/models/node-path";

export class NodeUpdatedEvent {
    constructor(public readonly node: FileExplorerNode,
                public readonly index: number,
                public readonly oldPath?: NodePath)
    {
    }
}
