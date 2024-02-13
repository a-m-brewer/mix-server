import { Injectable } from '@angular/core';
import {ISignalrClient} from "./signalr-client.interface";
import {HubConnection} from "@microsoft/signalr";
import {Observable, Subject} from "rxjs";
import {
  FileExplorerNodeDeletedDto,
  FileExplorerNodeUpdatedDto,
  FolderNodeResponse,
  NodeResponse
} from "../../generated-clients/mix-server-clients";
import {FileExplorerNodeConverterService} from "../converters/file-explorer-node-converter.service";
import {
  FileExplorerPopulatedFolderNode
} from "../../main-content/file-explorer/models/file-explorer-populated-folder-node";
import {FileExplorerNode} from "../../main-content/file-explorer/models/file-explorer-node";
import {NodeUpdatedEvent} from "./models/node-updated-event";
import {NodeDeletedEvent} from "./models/node-deleted-event";

@Injectable({
  providedIn: 'root'
})
export class FolderSignalrClientService implements ISignalrClient {
  private _folderSortedSubject$ = new Subject<FileExplorerPopulatedFolderNode>();
  private _nodeAddedSubject$ = new Subject<FileExplorerNode>();
  private _nodeUpdatedSubject$ = new Subject<NodeUpdatedEvent>();
  private _nodeDeletedSubject$ = new Subject<NodeDeletedEvent>();

  constructor(private _folderNodeConverter: FileExplorerNodeConverterService) { }

  public folderSorted$(): Observable<FileExplorerPopulatedFolderNode> {
    return this._folderSortedSubject$.asObservable();
  }

  public nodeAdded$(): Observable<FileExplorerNode> {
    return this._nodeAddedSubject$.asObservable();
  }

  public nodeUpdated$(): Observable<NodeUpdatedEvent> {
    return this._nodeUpdatedSubject$.asObservable();
  }

  public nodeDeleted$(): Observable<NodeDeletedEvent> {
    return this._nodeDeletedSubject$.asObservable();
  }

  registerMethods(connection: HubConnection): void {
    connection.on(
      'FolderSorted',
      (dtoObject: object) => this.handleFolderSorted(FolderNodeResponse.fromJS(dtoObject)));

    connection.on(
      'FileExplorerNodeAdded',
      (obj: object) => this.handleFileExplorerNodeAdded(NodeResponse.fromJS(obj))
    )

    connection.on(
      'FileExplorerNodeUpdated',
      (obj: object) => this.handleFileExplorerNodeUpdated(FileExplorerNodeUpdatedDto.fromJS(obj))
    );

    connection.on(
      'FileExplorerNodeDeleted',
      (obj: object) => this.handleFileExplorerNodeDeleted(FileExplorerNodeDeletedDto.fromJS(obj))
    )
  }

  private handleFolderSorted(dto: FolderNodeResponse): void {
    const converted = this._folderNodeConverter.fromDto(dto);

    this._folderSortedSubject$.next(converted);
  }

  private handleFileExplorerNodeAdded(dto: NodeResponse): void {
    const node = this._folderNodeConverter.fromResponse(dto);
    this._nodeAddedSubject$.next(node);
  }

  private handleFileExplorerNodeUpdated(dto: FileExplorerNodeUpdatedDto): void {
    const node = this._folderNodeConverter.fromResponse(dto.node);
    this._nodeUpdatedSubject$.next(new NodeUpdatedEvent(node, dto.oldAbsolutePath));
  }

  private handleFileExplorerNodeDeleted(dto: FileExplorerNodeDeletedDto): void {
    const parent = this._folderNodeConverter.fromFolderResponse(dto.parent);
    this._nodeDeletedSubject$.next(new NodeDeletedEvent(parent, dto.absolutePath));
  }
}
