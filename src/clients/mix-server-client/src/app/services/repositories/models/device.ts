import {ClientType, DeviceType} from "../../../generated-clients/mix-server-clients";
import {FileExplorerFileNode} from "../../../main-content/file-explorer/models/file-explorer-file-node";
import {DeviceState} from "./device-state";

export class Device {
  constructor(public id: string,
              public clientType: ClientType,
              public deviceType: DeviceType,
              public lastSeen: Date,
              public interactedWith: boolean,
              public isCurrentDevice: boolean,
              public online: boolean,
              public capabilities: { [mimeType: string]: boolean },
              public brand: string | undefined,
              public browserName: string | undefined,
              public model: string | undefined,
              public osName: string | undefined,
              public osVersion: string | undefined) {
  }

  public get displayName(): string {
    return `${this.brand ?? ''} ${this.model ?? ''} ${this.osName ?? ''} ${this.osVersion ?? ''} ${this.browserName ?? ''}`
  }

  public get icon(): string {
    return 'devices'
  }

  canPlay(file?: FileExplorerFileNode | null): boolean {
    return !!file && (file.hasCompletedTranscode || (this.capabilities[file.metadata.mimeType] ?? false));
  }

  updateFromState(state: DeviceState, currentUserId: string | null | undefined) {
    this.interactedWith = state.interactedWith && state.lastInteractedWith === currentUserId;
    this.online = state.online && state.lastInteractedWith === currentUserId;
    this.capabilities = state.capabilities;
  }
}
