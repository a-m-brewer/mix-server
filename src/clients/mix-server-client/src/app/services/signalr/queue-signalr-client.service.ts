import { Injectable } from '@angular/core';
import {ISignalrClient} from "./signalr-client.interface";
import {HubConnection} from "@microsoft/signalr";
import {QueueSnapshotDto} from "../../generated-clients/mix-server-clients";
import {QueueConverterService} from "../converters/queue-converter.service";
import {Observable, Subject} from "rxjs";
import {Queue} from "../repositories/models/queue";

@Injectable({
  providedIn: 'root'
})
export class QueueSignalrClientService implements ISignalrClient{
  private _queueSubject$ = new Subject<Queue>();

  constructor(private _queueConverter: QueueConverterService)
  {
  }

  public registerMethods(connection: HubConnection): void {
    connection.on(
      'CurrentQueueUpdated',
      (dtoObject: object) => this.handleCurrentQueueUpdated(QueueSnapshotDto.fromJS(dtoObject)));
  }

  public queue$(): Observable<Queue> {
    return this._queueSubject$.asObservable();
  }

  private handleCurrentQueueUpdated(dto: QueueSnapshotDto): void {
    const converted = this._queueConverter.fromDto(dto);

    this._queueSubject$.next(converted);
  }
}
