import {Injectable} from '@angular/core';
import {
  FileExplorerNodeType,
  FileNodeResponse, FolderInfoResponse,
  FolderNodeResponse,
  FolderSortDto,
  FolderSortMode,
  NodeResponse
} from "../../generated-clients/mix-server-clients";
import {FileExplorerNode} from "../../main-content/file-explorer/models/file-explorer-node";
import {FileExplorerFileNode} from "../../main-content/file-explorer/models/file-explorer-file-node";
import {FileExplorerFolderNode} from "../../main-content/file-explorer/models/file-explorer-folder-node";
import {FolderSort} from "../../main-content/file-explorer/models/folder-sort";
import {
  FileExplorerPopulatedFolderNode
} from "../../main-content/file-explorer/models/file-explorer-populated-folder-node";
import {FileExplorerFolderSortMode} from "../../main-content/file-explorer/enums/file-explorer-folder-sort-mode";

@Injectable({
  providedIn: 'root'
})
export class FileExplorerNodeConverterService {

  constructor() { }

  public fromDto(dto: FolderNodeResponse): FileExplorerPopulatedFolderNode {
    const folderNode = this.fromFolderResponse(dto);

    return new FileExplorerPopulatedFolderNode(folderNode, dto.children.map(m => this.fromResponse(m)));
  }

  public fromResponse(response: NodeResponse): FileExplorerNode {
    switch (response.type) {
      case FileExplorerNodeType.File:
        return this.fromFileResponse(response as FileNodeResponse);
      case FileExplorerNodeType.Folder:
        return this.fromFolderResponse(response as FolderNodeResponse);
    }
  }

  public fromFileResponse(file: FileNodeResponse): FileExplorerFileNode {
    return new FileExplorerFileNode(file.name, file.nameIdentifier, file.absolutePath, file.exists, file.playbackSupported, undefined, this.fromFolderInfoResponse(file.parent));
  }

  public fromFolderResponse(response: FolderNodeResponse): FileExplorerFolderNode {
    return new FileExplorerFolderNode(response.name, response.nameIdentifier, response.exists, response.absolutePath, this.fromFolderSortDto(response.sort), this.fromParentAbsolutePath(response.info.parentAbsolutePath));
  }

  public fromFolderInfoResponse(response: FolderInfoResponse): FileExplorerFolderNode {
    return new FileExplorerFolderNode(response.name, response.nameIdentifier, response.exists, response.absolutePath, this.fromFolderSortDto(response.sort), this.fromParentAbsolutePath(response.parentAbsolutePath));
  }

  private fromParentAbsolutePath(absolutePath?: string | null): FileExplorerFolderNode | null {
    return new FileExplorerFolderNode('..', '', true, absolutePath, FolderSort.Default, null);
  }

  private fromFolderSortDto(dto: FolderSortDto): FolderSort {
    return new FolderSort(dto.descending, this.fromFolderSortMode(dto.sortMode));
  }

  private fromFolderSortMode(sortMode: FolderSortMode): FileExplorerFolderSortMode {
    switch (sortMode) {
      case FolderSortMode.Created:
        return FileExplorerFolderSortMode.Created;
      case FolderSortMode.Name:
      default:
        return FileExplorerFolderSortMode.Name;
    }
  }
}
