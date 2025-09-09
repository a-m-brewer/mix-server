import { Injectable } from '@angular/core';
import {ISignalrClient} from "./signalr-client.interface";
import {HubConnection} from "@microsoft/signalr";
import {QueueConverterService} from "../converters/queue-converter.service";
import {QueueItemsAddedDto, QueueItemsRemovedDto, QueuePositionDto} from "../../generated-clients/mix-server-clients";
import {Subject} from "rxjs";
import {QueuePosition} from "../repositories/models/QueuePosition";
import {QueueItemsAddedEvent} from "./models/queue-items-added-event";
import {QueueItemsRemovedEvent} from "./models/queue-items-removed-event";

@Injectable({
  providedIn: 'root'
})
export class QueueSignalrClientService implements ISignalrClient{
  private _queuePositionChanged$ = new Subject<QueuePosition>();
  private _queueFolderChanged$ = new Subject<QueuePosition>();
  private _queueItemsAdded$ = new Subject<QueueItemsAddedEvent>();
  private _queueItemsRemoved$ = new Subject<QueueItemsRemovedEvent>();


  constructor(private _queueConverter: QueueConverterService)
  {
  }

  public get queuePositionChanged$() {
    return this._queuePositionChanged$.asObservable();
  }

  public get queueFolderChanged$() {
    return this._queueFolderChanged$.asObservable();
  }

  public get queueItemsAdded$() {
    return this._queueItemsAdded$.asObservable();
  }

  public get queueItemsRemoved$() {
    return this._queueItemsRemoved$.asObservable();
  }

  public registerMethods(connection: HubConnection): void {
    connection.on(
      'QueuePositionChanged',
      (obj: object)=> this.handleQueuePositionChanged(QueuePositionDto.fromJS(obj))
    );

    connection.on(
      'QueueFolderChanged',
      (obj: object)=> this.handleQueueFolderChanged(QueuePositionDto.fromJS(obj))
    );

    connection.on(
      'QueueItemsAdded',
      (obj: object)=> this.handleQueueItemsAdded(QueueItemsAddedDto.fromJS(obj))
    );

    connection.on(
      'QueueItemsRemoved',
      (obj: object)=> this.handleQueueItemsRemoved(QueueItemsRemovedDto.fromJS(obj))
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

  private handleQueueItemsAdded(queueItemsAddedDto: QueueItemsAddedDto) {
    const position = this._queueConverter.toQueuePosition(queueItemsAddedDto.currentPosition);
    const added = queueItemsAddedDto.addedItems.map(item => this._queueConverter.toQueueItem(item));
    this._queueItemsAdded$.next({position, added});
  }

  private handleQueueItemsRemoved(queueItemsRemovedDto: QueueItemsRemovedDto) {
    const position = this._queueConverter.toQueuePosition(queueItemsRemovedDto.currentPosition);
    const removed = queueItemsRemovedDto.removedItemIds;
    this._queueItemsRemoved$.next({position, removed});
  }
}
