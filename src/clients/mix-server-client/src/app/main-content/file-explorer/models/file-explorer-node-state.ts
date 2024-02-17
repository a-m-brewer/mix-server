export interface FileExplorerNodeStateInterface {
  state: FileExplorerNodeState;
}

export class FileExplorerNodeState implements FileExplorerNodeStateInterface{
  

  constructor(public absolutePath: string) {
  }
}
