import {Injectable} from '@angular/core';
import {RoleRepositoryService} from "./role-repository.service";
import {AuthenticationService} from "../auth/authentication.service";
import {BehaviorSubject, combineLatestWith, Observable} from "rxjs";
import {User} from "./models/user";
import {ServerConnectionState} from "../auth/enums/ServerConnectionState";
import {UserClient} from "../../generated-clients/mix-server-clients";
import {UserConverterService} from "../converters/user-converter.service";
import {ToastService} from "../toasts/toast-service";
import {Role} from "../../generated-clients/mix-server-clients";
import {UserSignalrClientService} from "../signalr/user-signalr-client.service";

@Injectable({
  providedIn: 'root'
})
export class UserRepositoryService {
  private _usersBehaviourSubject$ = new BehaviorSubject<Array<User>>([]);

  constructor(
    authService: AuthenticationService,
    roleRepository: RoleRepositoryService,
    toastService: ToastService,
    userConverter: UserConverterService,
    userClient: UserClient,
    userSignalrClient: UserSignalrClientService) {

    authService.serverConnectionStatus$
      .pipe(combineLatestWith(roleRepository.inRole$(Role.Administrator)))
      .subscribe(([serverConnectionState, isAdmin]) => {
        if (serverConnectionState === ServerConnectionState.Connected && isAdmin) {
          userClient.getAll()
            .subscribe({
              next: response => {
                const users = userConverter.fromGetAllResponse(response)
                this._usersBehaviourSubject$.next(users);
              },
              error: err => {
                toastService.logServerError(err, 'Failed to fetch users')
                this._usersBehaviourSubject$.next([]);
              }
            });
        }
        else {
          this._usersBehaviourSubject$.next([]);
        }
      });

    userSignalrClient.userAdded$
      .subscribe(user => {
        const nextUsers = [...this._usersBehaviourSubject$.value, user];
        this._usersBehaviourSubject$.next(nextUsers);
      });

    userSignalrClient.userUpdated$
      .subscribe(user => {
        const nextUsers = this._usersBehaviourSubject$.value.map(u => u.id === user.id ? user : u);
        this._usersBehaviourSubject$.next(nextUsers);
      });

    userSignalrClient.userDeleted$
      .subscribe(userId => {
        const nextUsers = this._usersBehaviourSubject$.value.filter(user => user.id !== userId);
        this._usersBehaviourSubject$.next(nextUsers);
      });
  }

  public get users$(): Observable<Array<User>> {
    return this._usersBehaviourSubject$.asObservable();
  }
}
