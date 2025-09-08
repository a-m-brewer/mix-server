import { Injectable } from '@angular/core';
import {ISignalrClient} from "./signalr-client.interface";
import {HubConnection} from "@microsoft/signalr";
import {QueueConverterService} from "../converters/queue-converter.service";

@Injectable({
  providedIn: 'root'
})
export class QueueSignalrClientService implements ISignalrClient{
  constructor(private _queueConverter: QueueConverterService)
  {
  }

  public registerMethods(connection: HubConnection): void {
  }
}
