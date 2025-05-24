import {FileExplorerNode} from "./file-explorer-node";
import {FileExplorerNodeType} from "../enums/file-explorer-node-type";
import {NodePath} from "./node-path";

export class FileExplorerFolderNode implements FileExplorerNode {
  constructor(public path: NodePath,
              public type: FileExplorerNodeType,
              public exists: boolean,
              public creationTimeUtc: Date,
              public belongsToRoot: boolean,
              public belongsToRootChild: boolean,
              public parent: FileExplorerFolderNode | undefined | null) {
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

    return this.path.isEqual(node.path);
  }

  public static get Default(): FileExplorerFolderNode {
    return new FileExplorerFolderNode(
      NodePath.Default,
      FileExplorerNodeType.Folder,
      false,
      new Date(),
      false,
      false,
      null);
  }

  public copy(): FileExplorerFolderNode {
    return new FileExplorerFolderNode(
      this.path.copy(),
      this.type,
      this.exists,
      this.creationTimeUtc,
      this.belongsToRoot,
      this.belongsToRootChild,
      this.parent ? this.parent.copy() : null
    );
  }
}
