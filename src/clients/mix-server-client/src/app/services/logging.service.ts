import { Injectable } from '@angular/core';
import {LogSignalrClientService} from "./signalr/log-signalr-client.service";
import {LogLevel} from "../generated-clients/mix-server-clients";

@Injectable({
  providedIn: 'root'
})
export class LoggingService {

  constructor(private _logClient: LogSignalrClientService) { }

  public debug(message: string) {
    this.log(LogLevel.Debug, message);
  }

  public info(message: string) {
    this.log(LogLevel.Information, message);
  }

  public warning(message: string) {
    this.log(LogLevel.Warning, message);
  }

  public error(message: string) {
    this.log(LogLevel.Error, message);
  }

  private log(level: LogLevel, message: string): void {
    try {
      this._logClient.log(level, message);
    }
    catch (e) {
    }
  }
}
