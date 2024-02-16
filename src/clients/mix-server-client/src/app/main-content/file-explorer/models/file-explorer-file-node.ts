import {FileExplorerNode} from "./file-explorer-node";
import {FileExplorerNodeType} from "../enums/file-explorer-node-type";
import {FileExplorerFolderInfo} from "./file-explorer-folder-info";

export class FileExplorerFileNode extends FileExplorerNode {
  constructor(name: string,
              exists: boolean,
              creationTimeUtc: Date,
              public mimeType: string,
              public playbackSupported: boolean,
              public parent: FileExplorerFolderInfo) {
    super(name, FileExplorerNodeType.File, exists, creationTimeUtc)
  }
}
