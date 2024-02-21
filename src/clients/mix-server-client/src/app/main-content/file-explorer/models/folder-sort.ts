import {FileExplorerFolderSortMode} from "../enums/file-explorer-folder-sort-mode";

export class FolderSort {
  constructor(public descending: boolean,
              public sortMode: FileExplorerFolderSortMode) {
  }

  public static get Default(): FolderSort {
    return new FolderSort(false, FileExplorerFolderSortMode.Name);
  }

  public copy(): FolderSort {
    return new FolderSort(this.descending, this.sortMode);
  }
}
