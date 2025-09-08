import { Injectable } from '@angular/core';
import {ISignalrClient} from "./signalr-client.interface";
import {HubConnection} from "@microsoft/signalr";
import {QueueConverterService} from "../converters/queue-converter.service";
import {QueuePositionDto} from "../../generated-clients/mix-server-clients";
import {Subject} from "rxjs";
import {QueuePosition} from "../repositories/models/QueuePosition";

@Injectable({
  providedIn: 'root'
})
export class QueueSignalrClientService implements ISignalrClient{
  private _queuePositionChanged$ = new Subject<QueuePosition>();
  private _queueFolderChanged$ = new Subject<QueuePosition>();

  constructor(private _queueConverter: QueueConverterService)
  {
  }

  public get queuePositionChanged$() {
    return this._queuePositionChanged$.asObservable();
  }

  public get queueFolderChanged$() {
    return this._queueFolderChanged$.asObservable();
  }

  public registerMethods(connection: HubConnection): void {
    connection.on(
      'QueuePositionChanged',
      (obj: object)=> this.handleQueuePositionChanged(QueuePositionDto.fromJS(obj))
    )

    connection.on(
      'QueueFolderChanged',
      (obj: object)=> this.handleQueueFolderChanged(QueuePositionDto.fromJS(obj))
    )
  }

  private handleQueuePositionChanged(queuePositionDto: QueuePositionDto) {
    const queuePosition = this._queueConverter.toQueuePosition(queuePositionDto);
    this._queuePositionChanged$.next(queuePosition);
  }

  private handleQueueFolderChanged(queuePositionDto: QueuePositionDto) {
    const queuePosition = this._queueConverter.toQueuePosition(queuePositionDto);
    this._queueFolderChanged$.next(queuePosition);
  }
}
