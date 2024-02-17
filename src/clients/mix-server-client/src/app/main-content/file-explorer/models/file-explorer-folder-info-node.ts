import {FileExplorerNodeType} from "../enums/file-explorer-node-type";


export interface FileExplorerFolderInfoNodeInterface {
  name: string;
  absolutePath: string;
  type: FileExplorerNodeType;
  exists: boolean;
  creationTimeUtc: Date;
  belongsToRoot: boolean;
  belongsToRootChild: boolean;
  disabled: boolean;
}

export class FileExplorerFolderInfoNode implements FileExplorerFolderInfoNodeInterface {
  constructor(public name: string,
              public absolutePath: string,
              public type: FileExplorerNodeType,
              public exists: boolean,
              public creationTimeUtc: Date,
              public belongsToRoot: boolean,
              public belongsToRootChild: boolean) {
    this.disabled = absolutePath.trim() === '' || !exists;
  }

  disabled: boolean;

  public copy(): FileExplorerFolderInfoNode {
    return new FileExplorerFolderInfoNode(
      this.name,
      this.absolutePath,
      this.type,
      this.exists,
      this.creationTimeUtc,
      this.belongsToRoot,
      this.belongsToRootChild
    );
  }
}
