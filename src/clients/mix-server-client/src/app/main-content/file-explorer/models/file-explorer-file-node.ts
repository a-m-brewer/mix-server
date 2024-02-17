import {FileExplorerNode} from "./file-explorer-node";
import {FileExplorerNodeType} from "../enums/file-explorer-node-type";
import {FileExplorerFolderNode} from "./file-explorer-folder-node";
import {FileExplorerNodeStateInterface} from "./file-explorer-node-state";

export class FileExplorerFileNode implements FileExplorerNode {
  constructor(public name: string,
              public absolutePath: string,
              public type: FileExplorerNodeType,
              public exists: boolean,
              public creationTimeUtc: Date,
              public mimeType: string,
              public playbackSupported: boolean,
              public parent: FileExplorerFolderNode,
              public state: FileExplorerNodeStateInterface) {
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
      this.mimeType,
      this.playbackSupported,
      this.parent.copy(),
      this.state
    );
  }
}
