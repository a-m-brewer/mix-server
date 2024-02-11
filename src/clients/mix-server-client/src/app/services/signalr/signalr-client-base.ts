import {HubConnection, HubConnectionState} from "@microsoft/signalr";

export abstract class SignalrClientBase {
  protected connection: HubConnection | null | undefined = null;

  protected send(methodName: string, value?: any): void {
    if (!this.connection || this.connection.state !== HubConnectionState.Connected) {
      return;
    }

    if (value) {
      this.connection.send(methodName, value).then();
    }
    else {
      this.connection.send(methodName).then();
    }
  }
}
