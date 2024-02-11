import {IDeviceStateDto} from "../../../generated-clients/mix-server-clients";

export class DeviceState implements IDeviceStateDto {
  constructor(public deviceId: string,
              public lastInteractedWith: string,
              public interactedWith: boolean) {
  }
}
