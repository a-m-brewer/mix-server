import {Injectable} from '@angular/core';
import {ISignalrClient} from "./signalr-client.interface";
import {HubConnection} from "@microsoft/signalr";
import {Observable, Subject} from "rxjs";
import {
  FileExplorerFolderResponse,
  FileExplorerNodeDeletedDto, FileExplorerNodeUpdatedDto, MediaInfoRemovedDto, MediaInfoUpdatedDto
} from "../../generated-clients/mix-server-clients";
import {FileExplorerNodeConverterService} from "../converters/file-explorer-node-converter.service";
import {NodeUpdatedEvent} from "./models/node-updated-event";
import {NodeDeletedEvent} from "./models/node-deleted-event";
import {FileExplorerFolder} from "../../main-content/file-explorer/models/file-explorer-folder";
import {MediaInfoRemovedEvent, MediaInfoUpdatedEvent} from "./models/media-info-event";
import {FileMetadataConverterService} from "../converters/file-metadata-converter.service";
import {NodePathConverterService} from "../converters/node-path-converter.service";

@Injectable({
  providedIn: 'root'
})
export class FolderSignalrClientService implements ISignalrClient {
  private _folderRefreshed$ = new Subject<FileExplorerFolder>();
  private _folderSortedSubject$ = new Subject<FileExplorerFolder>();
  private _nodeUpdatedSubject$ = new Subject<NodeUpdatedEvent>();
  private _nodeDeletedSubject$ = new Subject<NodeDeletedEvent>();
  private _mediaInfoUpdatedSubject$ = new Subject<MediaInfoUpdatedEvent>();
  private _mediaInfoRemovedSubject$ = new Subject<MediaInfoRemovedEvent>();

  constructor(private _fileMetadataConverter: FileMetadataConverterService,
              private _folderNodeConverter: FileExplorerNodeConverterService,
              private _nodePathConverter: NodePathConverterService) {
  }

  public folderRefreshed$(): Observable<FileExplorerFolder> {
    return this._folderRefreshed$.asObservable();
  }

  public folderSorted$(): Observable<FileExplorerFolder> {
    return this._folderSortedSubject$.asObservable();
  }

  public nodeUpdated$(): Observable<NodeUpdatedEvent> {
    return this._nodeUpdatedSubject$.asObservable();
  }

  public nodeDeleted$(): Observable<NodeDeletedEvent> {
    return this._nodeDeletedSubject$.asObservable();
  }

  public mediaInfoUpdated$(): Observable<MediaInfoUpdatedEvent> {
    return this._mediaInfoUpdatedSubject$.asObservable();
  }

  public mediaInfoRemoved$(): Observable<MediaInfoRemovedEvent> {
    return this._mediaInfoRemovedSubject$.asObservable();
  }

  registerMethods(connection: HubConnection): void {
    connection.on(
      'FolderRefreshed',
      (dtoObject: object) => this.handleFolderRefreshed(FileExplorerFolderResponse.fromJS(dtoObject)));

    connection.on(
      'FolderSorted',
      (dtoObject: object) => this.handleFolderSorted(FileExplorerFolderResponse.fromJS(dtoObject)));

    connection.on(
      'FileExplorerNodeUpdated',
      (obj: object) => this.handleFileExplorerNodeUpdated(FileExplorerNodeUpdatedDto.fromJS(obj))
    );

    connection.on(
      'FileExplorerNodeDeleted',
      (obj: object) => this.handleFileExplorerNodeDeleted(FileExplorerNodeDeletedDto.fromJS(obj))
    );

    connection.on(
      'MediaInfoUpdated',
      (obj: object) => this.handleMediaInfoUpdated(MediaInfoUpdatedDto.fromJS(obj))
    );

    connection.on(
      'MediaInfoRemoved',
      (obj: object) => this.handleMediaInfoRemoved(MediaInfoRemovedDto.fromJS(obj))
    );
  }

  private handleFolderRefreshed(dto: FileExplorerFolderResponse): void {
    const converted = this._folderNodeConverter.fromFileExplorerFolder(dto);

    this._folderRefreshed$.next(converted);
  }

  private handleFolderSorted(dto: FileExplorerFolderResponse): void {
    const converted = this._folderNodeConverter.fromFileExplorerFolder(dto);

    this._folderSortedSubject$.next(converted);
  }

  private handleFileExplorerNodeUpdated(dto: FileExplorerNodeUpdatedDto): void {
    const node = this._folderNodeConverter.fromFileExplorerNode(dto.node);
    const oldPath = dto.oldPath && this._nodePathConverter.fromDto(dto.oldPath);
    this._nodeUpdatedSubject$.next(new NodeUpdatedEvent(node, dto.index, oldPath));
  }

  private handleFileExplorerNodeDeleted(dto: FileExplorerNodeDeletedDto): void {
    const parent = this._folderNodeConverter.fromFileExplorerFolderNode(dto.parent);
    const nodePath = this._nodePathConverter.fromDto(dto.nodePath);
    this._nodeDeletedSubject$.next(new NodeDeletedEvent(parent, nodePath));
  }

  private handleMediaInfoUpdated(dto: MediaInfoUpdatedDto): void {
    console.log('MediaInfoUpdated');
    const event: MediaInfoUpdatedEvent = {
      mediaInfo: dto.mediaInfo.map(item => {
        return {
          nodePath: this._nodePathConverter.fromDto(item.nodePath),
          info: this._fileMetadataConverter.fromMediaInfoDto(item)
        }
      })
    };
    this._mediaInfoUpdatedSubject$.next(event);
  }

  private handleMediaInfoRemoved(dto: MediaInfoRemovedDto): void {
    console.log('MediaInfoRemoved');
    const event: MediaInfoRemovedEvent = {
      nodePaths: dto.nodePaths.map(item => {
        return this._nodePathConverter.fromDto(item)
      })
    };
    this._mediaInfoRemovedSubject$.next(event);
  }
}
