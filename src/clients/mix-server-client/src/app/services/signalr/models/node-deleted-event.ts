import {FileExplorerFolderNode} from "../../../main-content/file-explorer/models/file-explorer-folder-node";
import {NodePath} from "../../../main-content/file-explorer/models/node-path";

export class NodeDeletedEvent {
    constructor(public readonly parent: FileExplorerFolderNode,
                public readonly nodePath: NodePath) {
    }
}
