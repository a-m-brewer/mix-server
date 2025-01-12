import {FileExplorerNode} from "./file-explorer-node";
import {FileExplorerNodeType} from "../enums/file-explorer-node-type";
import {FileExplorerFolderNode} from "./file-explorer-folder-node";
import {FileMetadata} from "./file-metadata";

export class FileExplorerFileNode implements FileExplorerNode {
  constructor(public name: string,
              public absolutePath: string,
              public type: FileExplorerNodeType,
              public exists: boolean,
              public creationTimeUtc: Date,
              public metadata: FileMetadata,
              public playbackSupported: boolean,
              public parent: FileExplorerFolderNode) {
    this.disabled = absolutePath.trim() === '' || !exists || !playbackSupported;
  }

  public disabled: boolean;

  public mdIcon: string = 'description';

  public isEqual(node: FileExplorerNode | null | undefined): boolean {
    if (!node) {
      return false;
    }

    if (!(node instanceof FileExplorerFileNode)) {
      return false;
    }

    return this.absolutePath === node.absolutePath;
  }

  public get playbackDisabled(): boolean {
    return !this.playbackSupported ||
      !this.exists
  }

  public copy(): FileExplorerFileNode {
    return new FileExplorerFileNode(
      this.name,
      this.absolutePath,
      this.type,
      this.exists,
      this.creationTimeUtc,
      this.metadata.copy(),
      this.playbackSupported,
      this.parent.copy()
    );
  }
}
