import {FileExplorerNodeType} from "../enums/file-explorer-node-type";
import {FileExplorerNode} from "./file-explorer-node";
import {FolderSort} from "./folder-sort";
import {FileExplorerFileNode} from "./file-explorer-file-node";

export class FileExplorerFolderNode extends FileExplorerNode {
  constructor(name: string,
              nameIdentifier: string,
              exists: boolean,
              absolutePath: string | null | undefined,
              public sort: FolderSort,
              public parentDirectory: FileExplorerFolderNode | null | undefined) {
    super(name, nameIdentifier, absolutePath, FileExplorerNodeType.Folder, exists, 'folder', undefined);
  }

  public get disabled(): boolean {
    return !!this.absolutePath && !this.exists;
  }

  public isEqual(other: FileExplorerNode): boolean {
    if (!(other instanceof FileExplorerFolderNode)) {
      return false
    }

    const otherAsType = other as FileExplorerFolderNode;

    if (!this.absolutePath || !otherAsType.absolutePath) {
      return false;
    }

    return this.absolutePath === otherAsType.absolutePath;
  }

  public static get Default(): FileExplorerFolderNode {
    return new FileExplorerFolderNode('', '', false, null, FolderSort.Default, null);
  }
}
