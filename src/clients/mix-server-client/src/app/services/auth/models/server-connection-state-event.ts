import {ServerConnectionState} from "../enums/ServerConnectionState";

export class ServerConnectionStateEvent {
  constructor(public state: ServerConnectionState,
              public reason: string) {
  }
}
