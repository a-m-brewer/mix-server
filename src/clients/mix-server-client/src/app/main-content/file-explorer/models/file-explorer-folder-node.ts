import {FileExplorerNode} from "./file-explorer-node";
import {FileExplorerNodeType} from "../enums/file-explorer-node-type";

export class FileExplorerFolderNode implements FileExplorerNode {
  constructor(public name: string,
              public absolutePath: string,
              public type: FileExplorerNodeType,
              public exists: boolean,
              public creationTimeUtc: Date,
              public belongsToRoot: boolean,
              public belongsToRootChild: boolean) {
  }

  public static get Default(): FileExplorerFolderNode {
    return new FileExplorerFolderNode(
      '',
      '',
      FileExplorerNodeType.Folder,
      false,
      new Date(),
      false,
      false
    );
  }
}
