import {Injectable} from '@angular/core';
import {
  FileExplorerFileNodeResponse,
  FileExplorerFolderNodeResponse,
  FileExplorerFolderResponse,
  FileExplorerNodeResponse,
  FolderSortDto,
  FolderSortMode,
} from "../../generated-clients/mix-server-clients";
import {FolderSort} from "../../main-content/file-explorer/models/folder-sort";
import {FileExplorerFolderSortMode} from "../../main-content/file-explorer/enums/file-explorer-folder-sort-mode";
import {FileExplorerNode} from "../../main-content/file-explorer/models/file-explorer-node";
import {FileExplorerFileNode} from "../../main-content/file-explorer/models/file-explorer-file-node";
import {FileExplorerFolderNode} from "../../main-content/file-explorer/models/file-explorer-folder-node";
import {FileExplorerFolder} from "../../main-content/file-explorer/models/file-explorer-folder";
import {FileMetadataConverterService} from "./file-metadata-converter.service";
import {AudioElementRepositoryService} from "../audio-player/audio-element-repository.service";

@Injectable({
  providedIn: 'root'
})
export class FileExplorerNodeConverterService {
  constructor(private _fileMetadataConverter: FileMetadataConverterService,
              private _audioElementRepository: AudioElementRepositoryService) {
  }

  public fromFileExplorerFolder(dto: FileExplorerFolderResponse): FileExplorerFolder {
    return new FileExplorerFolder(
      this.fromFileExplorerFolderNode(dto.node),
      dto.children.map(child => this.fromFileExplorerNode(child)),
      this.fromFolderSortDto(dto.sort)
    );
  }

  public fromFileExplorerNode(dto: FileExplorerNodeResponse): FileExplorerNode {
    if (dto instanceof FileExplorerFileNodeResponse) {
      return this.fromFileExplorerFileNode(dto);
    }

    if (dto instanceof FileExplorerFolderNodeResponse) {
      return this.fromFileExplorerFolderNode(dto);
    }

    throw new Error(`Unknown FileExplorerNodeResponse type: ${dto}`);
  }

  public fromFileExplorerFileNode(dto: FileExplorerFileNodeResponse): FileExplorerFileNode {
    const metadata = this._fileMetadataConverter.fromResponse(dto.metadata);
    const clientPlaybackSupported = this._audioElementRepository.audio.canPlayType(metadata.mimeType) !== '';

    return new FileExplorerFileNode(
      dto.name,
      dto.absolutePath,
      dto.type,
      dto.exists,
      dto.creationTimeUtc,
      metadata,
      dto.playbackSupported,
      clientPlaybackSupported,
      this.fromFileExplorerFolderNode(dto.parent)
    );
  }

  public fromFileExplorerFolderNode(dto: FileExplorerFolderNodeResponse): FileExplorerFolderNode {
    return new FileExplorerFolderNode(
      dto.name,
      dto.absolutePath,
      dto.type,
      dto.exists,
      dto.creationTimeUtc,
      dto.belongsToRoot,
      dto.belongsToRootChild,
      dto.parent ? this.fromFileExplorerFolderNode(dto.parent) : null
    );
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
