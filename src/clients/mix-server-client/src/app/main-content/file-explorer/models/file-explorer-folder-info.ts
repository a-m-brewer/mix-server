export class FileExplorerFolderInfo {
  constructor(public name: string,
              public absolutePath: string,
              public parentAbsolutePath: string,
              public exists: boolean,
              public belongsToRoot: boolean,
              public belongsToRootChild: boolean,
              public creationTimeUtc: Date) {
  }
}
