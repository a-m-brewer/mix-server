import {FileExplorerNodeType} from "../enums/file-explorer-node-type";

export class FileExplorerNode {
  constructor(public name: string,
              public type: FileExplorerNodeType,
              public exists: boolean,
              public creationTimeUtc: Date) {
  }
}
