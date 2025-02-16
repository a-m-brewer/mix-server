import {Injectable} from '@angular/core';
import {ISignalrClient} from "./signalr-client.interface";
import {HubConnection} from "@microsoft/signalr";
import {Observable, Subject} from "rxjs";
import {
  FileExplorerFolderResponse,
  FileExplorerNodeDeletedDto, FileExplorerNodeUpdatedDto
} from "../../generated-clients/mix-server-clients";
import {FileExplorerNodeConverterService} from "../converters/file-explorer-node-converter.service";
import {NodeUpdatedEvent} from "./models/node-updated-event";
import {NodeDeletedEvent} from "./models/node-deleted-event";
import {FileExplorerFolder} from "../../main-content/file-explorer/models/file-explorer-folder";

@Injectable({
  providedIn: 'root'
})
export class FolderSignalrClientService implements ISignalrClient {
  private _folderRefreshed$ = new Subject<FileExplorerFolder>();
  private _folderSortedSubject$ = new Subject<FileExplorerFolder>();
  private _nodeUpdatedSubject$ = new Subject<NodeUpdatedEvent>();
  private _nodeDeletedSubject$ = new Subject<NodeDeletedEvent>();

  constructor(private _folderNodeConverter: FileExplorerNodeConverterService) {
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
    )
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
    console.log('updated node', node);
    this._nodeUpdatedSubject$.next(new NodeUpdatedEvent(node, dto.index, dto.oldAbsolutePath));
  }

  private handleFileExplorerNodeDeleted(dto: FileExplorerNodeDeletedDto): void {
    const parent = this._folderNodeConverter.fromFileExplorerFolderNode(dto.parent);
    this._nodeDeletedSubject$.next(new NodeDeletedEvent(parent, dto.absolutePath));
  }
}
