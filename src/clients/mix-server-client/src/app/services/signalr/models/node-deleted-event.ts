import {FileExplorerFolderNode} from "../../../main-content/file-explorer/models/file-explorer-folder-node";

export class NodeDeletedEvent {
    constructor(public readonly parent: FileExplorerFolderNode,
                public readonly absolutePath: string) {
    }
}
