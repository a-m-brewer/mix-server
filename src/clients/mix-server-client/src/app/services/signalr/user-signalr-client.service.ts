import {Injectable} from '@angular/core';
import {SignalrClientBase} from "./signalr-client-base";
import {ISignalrClient} from "./signalr-client.interface";
import {HubConnection} from '@microsoft/signalr';
import {User} from "../repositories/models/user";
import {Observable, Subject} from "rxjs";
import {UserDeletedDto, UserDto} from "../../generated-clients/mix-server-clients";
import {UserConverterService} from "../converters/user-converter.service";

@Injectable({
  providedIn: 'root'
})
export class UserSignalrClientService extends SignalrClientBase implements ISignalrClient {
  private _userAdded$ = new Subject<User>();
  private _userUpdated$ = new Subject<User>();
  private _userDeleted$ = new Subject<string>();

  constructor(private _userConverter: UserConverterService) {
    super();
  }

  public get userAdded$(): Observable<User> {
    return this._userAdded$.asObservable();
  }

  public get userUpdated$(): Observable<User> {
    return this._userUpdated$.asObservable();
  }

  public get userDeleted$(): Observable<string> {
    return this._userDeleted$.asObservable();
  }

  registerMethods(connection: HubConnection): void {
    this.connection = connection;

    this.connection.on(
      'UserAdded',
      (obj: object) => this.handleUserAdded(UserDto.fromJS(obj)));

    this.connection.on(
      'UserUpdated',
      (obj: object) => this.handleUserUpdated(UserDto.fromJS(obj)));

    this.connection.on(
      'UserDeleted',
      (obj: object) => this.handleUserDeleted(UserDeletedDto.fromJS(obj)));
  }

  private handleUserAdded(dto: UserDto): void {
    const user = this._userConverter.fromDto(dto);
    this._userAdded$.next(user);
  }

  private handleUserUpdated(dto: UserDto) {
    const user = this._userConverter.fromDto(dto);
    this._userUpdated$.next(user);
  }

  private handleUserDeleted(dto: UserDeletedDto): void {
    this._userDeleted$.next(dto.userId);
  }
}
