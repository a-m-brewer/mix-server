import { Injectable } from '@angular/core';
import {BehaviorSubject, filter, firstValueFrom, map, Observable} from "rxjs";
import {Device} from "./models/device";
import {AuthenticationService} from "../auth/authentication.service";
import {DeviceClient, SetDeviceInteractionCommand, UserClient} from "../../generated-clients/mix-server-clients";
import {ToastService} from "../toasts/toast-service";
import {DeviceConverterService} from "../converters/device-converter.service";
import {DevicesSignalrClientService} from "../signalr/devices-signalr-client.service";
import {cloneDeep} from "lodash";

@Injectable({
  providedIn: 'root'
})
export class DeviceRepositoryService {
  private _devicesBehaviourSubject$ = new BehaviorSubject<Device[]>([]);

  constructor(private _authenticationService: AuthenticationService,
              _deviceConverter: DeviceConverterService,
              private  _deviceClient: DeviceClient,
              private _deviceSignalRClient: DevicesSignalrClientService,
              private _toastService: ToastService) {
    _authenticationService.connected$
      .subscribe(connected => {
        if (connected) {
          this._deviceClient.devices()
            .subscribe({
              next: dto => {
                const devices = _deviceConverter.fromDtoList(dto.devices);
                this.next(devices);
              },
              error: err => this._toastService.logServerError(err, 'Failed to fetch user devices')
            });
        }
      });

    _deviceSignalRClient.deviceUpdated$
      .subscribe(device => {
        const devices = cloneDeep(this._devicesBehaviourSubject$.value).filter(f => f.id !== device.id);

        devices.unshift(device);

        this.next(devices);
      });

    _deviceSignalRClient.deviceStateUpdated$
      .subscribe(state => {
        const devices = cloneDeep(this._devicesBehaviourSubject$.value);

        const device = devices.find(f => f.id === state.deviceId);

        if (!device) {
          return;
        }

        device.updateFromState(state, _authenticationService.accessToken?.userId);

        this.next(devices);
      })

    _deviceSignalRClient.deviceDeleted$
      .subscribe(deviceId => {
        const devices = cloneDeep(this._devicesBehaviourSubject$.value).filter(f => f.id !== deviceId);

        this.next(devices);

        if (deviceId === _authenticationService.deviceId) {
          _authenticationService.logout();
        }
      });
  }

  public get devices$(): Observable<Device[]> {
    return this._devicesBehaviourSubject$.asObservable();
  }

  public get onlineDevices$(): Observable<Device[]> {
    return this._devicesBehaviourSubject$
      // Require the device is interacted with unless its this device because to do anything they would have to interact anyway
      .pipe(map(m => this.filterOnlineDevices(m)));
  }

  public get currentDevice$(): Observable<Device | null | undefined> {
    return this._devicesBehaviourSubject$
      .pipe(map(devices => devices.find(f => f.id === this._authenticationService.deviceId)));
  }

  public getDevice(deviceId: string | null | undefined): Device | null | undefined {
    return this._devicesBehaviourSubject$
      .getValue()
      .find(d => d.id === deviceId);
  }

  public delete(device: Device): void {
    this._deviceClient.deleteDevice(device.id)
      .subscribe({
        error: err => this._toastService.logServerError(err, `Failed to delete device: ${device.displayName}`)
      })
  }

  public setUserInteractedWithPage(interacted: boolean): void {
    const device = this._devicesBehaviourSubject$.getValue()
      .find(f => f.id === this._authenticationService.deviceId);

    if (!device) {
      return;
    }

    if (device.interactedWith === interacted) {
      return;
    }

    if (interacted) {
      firstValueFrom(this._deviceClient.setDeviceInteracted(new SetDeviceInteractionCommand({
        interacted
      })))
        .catch(err => this._toastService.logServerError(err, 'Failed to set device interacted'));
    }
    else {
      this._deviceSignalRClient.pageClosed();
    }
  }

  private next(devices: Device[]): void {
    this._devicesBehaviourSubject$.next(devices);
  }

  private filterOnlineDevices(devices: Device[]): Device[] {
    return devices.filter(f => (f.online && (f.interactedWith || f.isCurrentDevice)) || f.id === this._authenticationService.deviceId);
  }
}
