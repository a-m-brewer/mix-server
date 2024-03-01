import {Injectable} from '@angular/core';
import {SignalrClientBase} from "./signalr-client-base";
import {ISignalrClient} from "./signalr-client.interface";
import {HubConnection} from '@microsoft/signalr';
import {DebugMessageDto, LogLevel} from "../../generated-clients/mix-server-clients";

@Injectable({
  providedIn: 'root'
})
export class LogSignalrClientService extends SignalrClientBase implements ISignalrClient {

  constructor() {
    super();
  }

  registerMethods(connection: HubConnection): void {
    this.connection = connection;
  }

  public log(level: LogLevel, message: string): void {
    this.send('Log', new DebugMessageDto({
      level,
      message
    }).toJSON());
  }
}
