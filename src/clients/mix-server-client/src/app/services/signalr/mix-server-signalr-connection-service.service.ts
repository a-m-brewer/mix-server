import {Inject, Injectable} from '@angular/core';
import {SignalrConnectionFactoryService} from "./signalr-connection-factory.service";
import {MIXSERVER_BASE_URL} from "../../generated-clients/mix-server-clients";
import {HubConnection, HubConnectionState} from "@microsoft/signalr";
import {SessionSignalrClientService} from "./session-signalr-client.service";
import {QueueSignalrClientService} from "./queue-signalr-client.service";
import {FolderSignalrClientService} from "./folder-signalr-client.service";
import {DevicesSignalrClientService} from "./devices-signalr-client.service";
import {AuthenticationService} from "../auth/authentication.service";
import {ServerConnectionState} from "../auth/enums/ServerConnectionState";
import {UserSignalrClientService} from "./user-signalr-client.service";

@Injectable({
  providedIn: 'root'
})
export class MixServerSignalrConnectionServiceService {
  private readonly _baseUrl: string;
  private _connection: HubConnection | undefined;

  constructor(private _authService: AuthenticationService,
              private _signalRConnectionFactory: SignalrConnectionFactoryService,
              private _deviceClient: DevicesSignalrClientService,
              private _folderClient: FolderSignalrClientService,
              private _queueClient: QueueSignalrClientService,
              private _sessionClient: SessionSignalrClientService,
              private _userClient: UserSignalrClientService,
              @Inject(MIXSERVER_BASE_URL) baseUrl: string) {
    this._baseUrl = baseUrl;
  }
  public async connect(): Promise<void> {
    this._connection = this._signalRConnectionFactory.buildConnection(`${this._baseUrl}/callbacks`);
    this._connection.onclose(err => this._authService.serverConnectionStatus = ServerConnectionState.Disconnected);
    this._connection.onreconnecting(err => this._authService.serverConnectionStatus = ServerConnectionState.Disconnected);
    this.registerClientMethods();

    await this._connection.start()
      .catch(() => this._authService.serverConnectionStatus = ServerConnectionState.Disconnected);
  }

  public async disconnect(): Promise<void> {
    if (!this._connection || this._connection.state === HubConnectionState.Disconnected) {
      return;
    }

    await this._connection.stop();
    this._connection = undefined;
  }

  private registerClientMethods(): void {
    if (!this._connection) {
      return;
    }

    this._deviceClient.registerMethods(this._connection);
    this._folderClient.registerMethods(this._connection);
    this._queueClient.registerMethods(this._connection);
    this._sessionClient.registerMethods(this._connection);
    this._userClient.registerMethods(this._connection);
  }
}
