import { Injectable } from '@angular/core';
import {DeviceDto, DeviceStateDto} from "../../generated-clients/mix-server-clients";
import {Device} from "../repositories/models/device";
import {DeviceState} from "../repositories/models/device-state";
import {AuthenticationService} from "../auth/authentication.service";

@Injectable({
  providedIn: 'root'
})
export class DeviceConverterService {

  constructor(private _authService: AuthenticationService) { }

  public fromDto(dto: DeviceDto): Device {
    return new Device(
      dto.id,
      dto.clientType,
      dto.deviceType,
      dto.lastSeen,
      dto.interactedWith,
      this._authService.deviceId == dto.id,
      dto.online,
      dto.capabilities,
      dto.brand,
      dto.browserName,
      dto.model,
      dto.osName,
      dto.osVersion);
  }

  public fromDtoList(dtos: DeviceDto[]): Device[] {
    return dtos.map(d => this.fromDto(d));
  }

  public fromStateDto(dto: DeviceStateDto): DeviceState {
    return new DeviceState(dto.deviceId, dto.lastInteractedWith, dto.interactedWith, dto.online, dto.capabilities);
  }
}
