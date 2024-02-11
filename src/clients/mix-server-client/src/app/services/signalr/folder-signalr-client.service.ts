import { Injectable } from '@angular/core';
import {ISignalrClient} from "./signalr-client.interface";
import {HubConnection} from "@microsoft/signalr";
import {Observable, Subject} from "rxjs";
import {FolderNodeResponse} from "../../generated-clients/mix-server-clients";
import {FileExplorerNodeConverterService} from "../converters/file-explorer-node-converter.service";
import {
  FileExplorerPopulatedFolderNode
} from "../../main-content/file-explorer/models/file-explorer-populated-folder-node";

@Injectable({
  providedIn: 'root'
})
export class FolderSignalrClientService implements ISignalrClient {
  private _folderSortedSubject$ = new Subject<FileExplorerPopulatedFolderNode>();

  constructor(private _folderNodeConverter: FileExplorerNodeConverterService) { }

  public folderSorted$(): Observable<FileExplorerPopulatedFolderNode> {
    return this._folderSortedSubject$.asObservable();
  }

  registerMethods(connection: HubConnection): void {
    connection.on(
      'FolderSorted',
      (dtoObject: object) => this.handleFolderSorted(FolderNodeResponse.fromJS(dtoObject)));
  }

  private handleFolderSorted(dto: FolderNodeResponse): void {
    const converted = this._folderNodeConverter.fromDto(dto);

    this._folderSortedSubject$.next(converted);
  }
}
