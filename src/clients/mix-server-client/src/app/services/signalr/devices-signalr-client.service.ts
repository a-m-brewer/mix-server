import { Injectable } from '@angular/core';
import {ISignalrClient} from "./signalr-client.interface";
import {HubConnection} from "@microsoft/signalr";
import {DeviceDeletedDto, DeviceDto, DeviceStateDto} from "../../generated-clients/mix-server-clients";
import {DeviceConverterService} from "../converters/device-converter.service";
import {Observable, Subject} from "rxjs";
import {Device} from "../repositories/models/device";
import {SignalrClientBase} from "./signalr-client-base";
import {DeviceState} from "../repositories/models/device-state";

@Injectable({
  providedIn: 'root'
})
export class DevicesSignalrClientService extends SignalrClientBase implements ISignalrClient {
  private _deviceUpdatedSubject$ = new Subject<Device>();
  private _deviceStateUpdatedSubject$ = new Subject<DeviceState>();
  private _deviceDeletedSubject$ = new Subject<string>();

  constructor(private _deviceConverter: DeviceConverterService) {
    super();
  }

  public get deviceUpdated$(): Observable<Device> {
    return this._deviceUpdatedSubject$.asObservable();
  }

  public get deviceStateUpdated$(): Observable<DeviceState> {
    return this._deviceStateUpdatedSubject$.asObservable();
  }

  public get deviceDeleted$(): Observable<string> {
    return this._deviceDeletedSubject$.asObservable();
  }

  public registerMethods(connection: HubConnection): void {
    this.connection = connection;

    connection.on(
      'DeviceUpdated',
      (dtoObject: object) => this.handleDeviceUpdated(DeviceDto.fromJS(dtoObject)));

    connection.on(
      'DeviceDeleted',
      (dtoObject: object) => this.handleDeviceDeleted(DeviceDeletedDto.fromJS(dtoObject))
    )

    connection.on(
      'DeviceStateUpdated',
      (obj: object) => this.handleDeviceStateUpdated(DeviceStateDto.fromJS(obj))
    )
  }

  private handleDeviceUpdated(dto: DeviceDto): void {
    const converted = this._deviceConverter.fromDto(dto);

    this._deviceUpdatedSubject$.next(converted);
  }

  private handleDeviceDeleted(dto: DeviceDeletedDto): void {
    this._deviceDeletedSubject$.next(dto.deviceId);
  }

  private handleDeviceStateUpdated(dto: DeviceStateDto) {
    const converted = this._deviceConverter.fromStateDto(dto);

    this._deviceStateUpdatedSubject$.next(converted);
  }

  public pageClosed(): void {
    this.send('PageClosed');
  }
}
