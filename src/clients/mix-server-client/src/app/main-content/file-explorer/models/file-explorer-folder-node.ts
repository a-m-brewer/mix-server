import {FileExplorerNode} from "./file-explorer-node";
import {FileExplorerNodeType} from "../enums/file-explorer-node-type";
import {FileExplorerNodeStateClass, FileExplorerNodeStateInterface} from "./file-explorer-node-state";

export class FileExplorerFolderNode implements FileExplorerNode {
  constructor(public name: string,
              public absolutePath: string,
              public type: FileExplorerNodeType,
              public exists: boolean,
              public creationTimeUtc: Date,
              public belongsToRoot: boolean,
              public belongsToRootChild: boolean,
              public parent: FileExplorerFolderNode | undefined | null,
              public state: FileExplorerNodeStateInterface) {
    this.disabled = !exists;
  }

  public disabled: boolean = false;

  public mdIcon: string = 'folder';

  public isEqual(node: FileExplorerNode | null | undefined): boolean {
    if (!node) {
      return false;
    }

    if (!(node instanceof FileExplorerFolderNode)) {
      return false;
    }

    return this.absolutePath === node.absolutePath;
  }

  public static get Default(): FileExplorerFolderNode {
    return new FileExplorerFolderNode(
      '',
      '',
      FileExplorerNodeType.Folder,
      false,
      new Date(),
      false,
      false,
      null,
      new FileExplorerNodeStateClass('')
    );
  }

  public copy(): FileExplorerFolderNode {
    return new FileExplorerFolderNode(
      this.name,
      this.absolutePath,
      this.type,
      this.exists,
      this.creationTimeUtc,
      this.belongsToRoot,
      this.belongsToRootChild,
      this.parent ? this.parent.copy() : null,
      this.state
    );
  }
}
