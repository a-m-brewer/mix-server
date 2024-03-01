import {ClientType, DeviceType} from "../../../generated-clients/mix-server-clients";

export class Device {
  constructor(public id: string,
              public clientType: ClientType,
              public deviceType: DeviceType,
              public lastSeen: Date,
              public interactedWith: boolean,
              public online: boolean,
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
}
