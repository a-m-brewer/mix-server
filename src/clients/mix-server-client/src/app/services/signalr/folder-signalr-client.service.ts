import {Injectable} from '@angular/core';
import {ISignalrClient} from "./signalr-client.interface";
import {HubConnection} from "@microsoft/signalr";
import {Observable, Subject} from "rxjs";
import {
  FolderScanStatusDto,
  MediaInfoRemovedDto,
  MediaInfoUpdatedDto,
  PagedFileExplorerFolderResponse
} from "../../generated-clients/mix-server-clients";
import {FileExplorerNodeConverterService} from "../converters/file-explorer-node-converter.service";
import {NodeUpdatedEvent} from "./models/node-updated-event";
import {MediaInfoRemovedEvent, MediaInfoUpdatedEvent} from "./models/media-info-event";
import {FileMetadataConverterService} from "../converters/file-metadata-converter.service";
import {NodePathConverterService} from "../converters/node-path-converter.service";
import {PagedFileExplorerFolder} from "../../main-content/file-explorer/models/paged-file-explorer-folder";

@Injectable({
  providedIn: 'root'
})
export class FolderSignalrClientService implements ISignalrClient {
  private _folderRefreshed$ = new Subject<PagedFileExplorerFolder>();
  private _folderSortedSubject$ = new Subject<PagedFileExplorerFolder>();
  private _mediaInfoUpdatedSubject$ = new Subject<MediaInfoUpdatedEvent>();
  private _mediaInfoRemovedSubject$ = new Subject<MediaInfoRemovedEvent>();
  private _folderScanStatusChangedSubject$ = new Subject<boolean>();

  constructor(private _fileMetadataConverter: FileMetadataConverterService,
              private _folderNodeConverter: FileExplorerNodeConverterService,
              private _nodePathConverter: NodePathConverterService) {
  }

  public folderRefreshed$(): Observable<PagedFileExplorerFolder> {
    return this._folderRefreshed$.asObservable();
  }

  public folderSorted$(): Observable<PagedFileExplorerFolder> {
    return this._folderSortedSubject$.asObservable();
  }

  public mediaInfoUpdated$(): Observable<MediaInfoUpdatedEvent> {
    return this._mediaInfoUpdatedSubject$.asObservable();
  }

  public mediaInfoRemoved$(): Observable<MediaInfoRemovedEvent> {
    return this._mediaInfoRemovedSubject$.asObservable();
  }

  public folderScanStatusChanged$(): Observable<boolean> {
    return this._folderScanStatusChangedSubject$.asObservable();
  }

  registerMethods(connection: HubConnection): void {
    connection.on(
      'FolderRefreshed',
      (dtoObject: object) => this.handleFolderRefreshed(PagedFileExplorerFolderResponse.fromJS(dtoObject)));

    connection.on(
      'FolderSorted',
      (dtoObject: object) => this.handleFolderSorted(PagedFileExplorerFolderResponse.fromJS(dtoObject)));

    connection.on(
      'MediaInfoUpdated',
      (obj: object) => this.handleMediaInfoUpdated(MediaInfoUpdatedDto.fromJS(obj))
    );

    connection.on(
      'MediaInfoRemoved',
      (obj: object) => this.handleMediaInfoRemoved(MediaInfoRemovedDto.fromJS(obj))
    );

    connection.on(
      'FolderScanStatusChanged',
      (obj: object) => this.handleFolderScanStatusChanged(FolderScanStatusDto.fromJS(obj))
    );
  }

  private handleFolderRefreshed(dto: PagedFileExplorerFolderResponse): void {
    const converted = this._folderNodeConverter.fromPagedFileExplorerFolder(dto);

    this._folderRefreshed$.next(converted);
  }

  private handleFolderSorted(dto: PagedFileExplorerFolderResponse): void {
    const converted = this._folderNodeConverter.fromPagedFileExplorerFolder(dto);

    this._folderSortedSubject$.next(converted);
  }

  private handleMediaInfoUpdated(dto: MediaInfoUpdatedDto): void {
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
    const event: MediaInfoRemovedEvent = {
      nodePaths: dto.nodePaths.map(item => {
        return this._nodePathConverter.fromDto(item)
      })
    };
    this._mediaInfoRemovedSubject$.next(event);
  }

  private handleFolderScanStatusChanged(dto: FolderScanStatusDto) {
    this._folderScanStatusChangedSubject$.next(dto.scanInProgress);
  }
}
