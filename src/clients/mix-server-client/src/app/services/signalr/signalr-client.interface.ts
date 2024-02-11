import {HubConnection} from "@microsoft/signalr";

export interface ISignalrClient {
  registerMethods(connection: HubConnection): void;
}
