import { Injectable } from '@angular/core';
import {BehaviorSubject, filter, map, Observable} from "rxjs";
import {Device} from "./models/device";
import {AuthenticationService} from "../auth/authentication.service";
import {UserClient} from "../../generated-clients/mix-server-clients";
import {ToastService} from "../toasts/toast-service";
import {DeviceConverterService} from "../converters/device-converter.service";
import {DevicesSignalrClientService} from "../signalr/devices-signalr-client.service";

@Injectable({
  providedIn: 'root'
})
export class DeviceRepositoryService {
  private _devicesBehaviourSubject$ = new BehaviorSubject<Device[]>([]);

  constructor(private _authenticationService: AuthenticationService,
              _deviceConverter: DeviceConverterService,
              private _deviceSignalRClient: DevicesSignalrClientService,
              private _toastService: ToastService,
              private _userClient: UserClient) {
    _authenticationService.connected$
      .subscribe(connected => {
        if (connected) {
          this._userClient.devices()
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
        const devices = [...this._devicesBehaviourSubject$.getValue()].filter(f => f.id !== device.id);

        devices.unshift(device);

        this.next(devices);
      });

    _deviceSignalRClient.deviceStateUpdated$
      .subscribe(state => {
        const devices = [...this._devicesBehaviourSubject$.getValue()];

        const deviceIndex = devices.findIndex(f => f.id === state.deviceId);

        if (deviceIndex === -1) {
          return;
        }

        devices[deviceIndex].interactedWith = state.interactedWith && _authenticationService.accessToken?.userId === state.lastInteractedWith;

        this.next(devices);
      })

    _deviceSignalRClient.deviceDeleted$
      .subscribe(deviceId => {
        const devices = [...this._devicesBehaviourSubject$.getValue()].filter(f => f.id !== deviceId);

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
      .pipe(map(m => m.filter(f => f.interactedWith || f.id === this._authenticationService.deviceId)));
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
    this._userClient.deleteDevice(device.id)
      .subscribe({
        error: err => this._toastService.logServerError(err, `Failed to delete device: ${device.displayName}`)
      })
  }

  public setUserInteractedWithPage(): void {
    const device = this._devicesBehaviourSubject$.getValue()
      .find(f => f.id === this._authenticationService.deviceId);

    if (!device || device.interactedWith) {
      return;
    }

    this._deviceSignalRClient.setUserInteractedWithPage();
  }

  private next(devices: Device[]): void {
    this._devicesBehaviourSubject$.next(devices);
  }
}
