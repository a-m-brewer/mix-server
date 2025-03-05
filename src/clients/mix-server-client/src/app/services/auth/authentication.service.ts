import {Injectable} from '@angular/core';
import {Router} from "@angular/router";
import {PageRoutes} from "../../page-routes.enum";
import {ApiException} from "../../generated-clients/mix-server-clients";
import {BehaviorSubject, distinctUntilChanged, map, Observable} from "rxjs";
import {JwtToken} from "./models/jwt-token";
import {
  LoginCommandResponse,
  LoginUserCommand,
  ProblemDetails,
  RefreshUserCommand,
  RefreshUserResponse, ResetPasswordCommand
} from "../../generated-clients/mix-server-clients";
import {ToastService} from "../toasts/toast-service";
import {ServerConnectionState} from "./enums/ServerConnectionState";
import {RoleRepositoryService} from "../repositories/role-repository.service";
import {ServerConnectionStateEvent} from "./models/server-connection-state-event";
import {UserApiService} from "../api.service";

@Injectable({
  providedIn: 'root'
})
export class AuthenticationService {
  private _connectionStatusBehaviourSubject$ = new BehaviorSubject<ServerConnectionStateEvent>(new ServerConnectionStateEvent(ServerConnectionState.Initializing, 'Class Initialization'));
  private _jwtTokenBehaviour$ = new BehaviorSubject<JwtToken | null>(null);

  private _refreshScheduleTimeout: number = 0;
  private _accessTokenKey: string = 'accessToken';
  private _refreshTokenKey: string = 'refreshToken';
  private _deviceIdKey: string = 'deviceId';

  constructor(private rolesRepository: RoleRepositoryService,
              private _router: Router,
              private _toastService: ToastService,
              private _userClient: UserApiService) {
    this.unauthorized$.subscribe(unauthorized => {
      if (unauthorized) {
        this._router.navigate([PageRoutes.Login])
          .catch((err) => this._toastService.logServerError(err, 'Failed to navigate'))
          .then();
      }
    });
  }

  public get serverConnectionStateEvent(): ServerConnectionStateEvent {
    return this._connectionStatusBehaviourSubject$.getValue();
  }

  public get serverConnectionStateEvent$(): Observable<ServerConnectionStateEvent> {
    return this._connectionStatusBehaviourSubject$.asObservable();
  }

  public get serverConnectionStatus(): ServerConnectionState {
    return this._connectionStatusBehaviourSubject$.getValue().state;
  }

  public get serverConnectionStatus$(): Observable<ServerConnectionState> {
    return this._connectionStatusBehaviourSubject$
      .pipe(map(s => s.state));
  }

  public get connected$(): Observable<boolean> {
    return this._connectionStatusBehaviourSubject$
      .pipe(map(s => s.state === ServerConnectionState.Connected))
      .pipe(distinctUntilChanged());
  }

  public get connected(): boolean {
    return this.serverConnectionStatus === ServerConnectionState.Connected;
  }

  private get unauthorized$(): Observable<boolean> {
    return this._connectionStatusBehaviourSubject$
      .pipe(map(s => s.state === ServerConnectionState.Unauthorized))
      .pipe(distinctUntilChanged());
  }

  public setServerConnectionStatus(state: ServerConnectionState, reason: string) {
    const current = this.serverConnectionStateEvent;
    if (state === current.state) {
      return;
    }

    if (state === ServerConnectionState.Disconnected && current.state === ServerConnectionState.Unauthorized) {
      return;
    }

    this._connectionStatusBehaviourSubject$.next(new ServerConnectionStateEvent(state, reason));
  }

  public async initialize(): Promise<ServerConnectionState> {
    const result = await this.performTokenRefresh();
    this.setServerConnectionStatus(result.state, result.reason);

    if (this.serverConnectionStatus === ServerConnectionState.Unauthorized) {
      return this.serverConnectionStatus;
    }

    this.startScheduleRefresh();

    return this.serverConnectionStatus;
  }

  public async login(username: string, password: string): Promise<void> {
    const deviceId = this.deviceId ?? undefined;

    const result = await this._userClient.request('Login',
      client => client.login(new LoginUserCommand({
        username,
        password,
        deviceId
      })), 'Failed to login');

    const response = result.result ?? this.handleAuthError(result.err);

    if (response instanceof LoginCommandResponse) {
      this.accessToken = new JwtToken(response.accessToken);
      this.refreshToken = response.refreshToken;
      this.deviceId = response.deviceId;
      this.rolesRepository.roles = response.roles;

      if (response.passwordResetRequired) {
        this.setServerConnectionStatus(ServerConnectionState.AwaitingPasswordReset, 'Password Reset Required')
        await this._router.navigate([PageRoutes.ResetPassword], {
          state: {
            loginPassword: password
          }
        });
      } else {
        this.setServerConnectionStatus(ServerConnectionState.Connected, 'Logged In');
        await this._router.navigate([PageRoutes.Files]);
      }

      this.startScheduleRefresh();

      return;
    }

    this.setServerConnectionStatus(response, 'Failed to login');
  }

  public async resetPassword(currentPassword: string,
                             newPassword: string,
                             newPasswordConfirmation: string): Promise<boolean> {
    const result = await this._userClient.request('ResetPassword',
      client => client.resetPassword(new ResetPasswordCommand({
        currentPassword,
        newPassword,
        newPasswordConfirmation
      })), 'Failed to reset password');

    const success = !result.err

    if (success) {
      this.setServerConnectionStatus(ServerConnectionState.Connected, 'Password Reset')
      await this._router.navigate([PageRoutes.Files]);
    }

    return success;
  }

  public logout(): void {
    this.cancelScheduledRefresh();
    this.accessToken = null;
    this.refreshToken = null;
    this.setServerConnectionStatus(ServerConnectionState.Unauthorized, 'Logged Out');
  }

  public get accessToken(): JwtToken | null {
    const localStorageAccessToken = localStorage.getItem(this._accessTokenKey);
    if (!localStorageAccessToken) {
      this.jwtToken = null;
      return null;
    }

    if (this.jwtToken === null || this.jwtToken.value !== localStorageAccessToken) {
      this.jwtToken = new JwtToken(localStorageAccessToken);
    }

    return this.jwtToken;
  }

  public get currentUserId$(): Observable<string | null | undefined> {
    return this._jwtTokenBehaviour$
      .pipe(map(m => m?.userId));
  }

  private set accessToken(value: JwtToken | null) {
    if (value) {
      this.jwtToken = value;
      localStorage.setItem(this._accessTokenKey, value.value);
    } else {
      this.jwtToken = null;
      localStorage.removeItem(this._accessTokenKey);
    }
  }

  private get jwtToken(): JwtToken | null {
    return this._jwtTokenBehaviour$.getValue();
  }

  private set jwtToken(token: JwtToken | null) {
    this._jwtTokenBehaviour$.next(token);
  }

  private get refreshToken(): string | null {
    return localStorage.getItem(this._refreshTokenKey);
  }

  private set refreshToken(value: string | null | undefined) {
    if (value) {
      localStorage.setItem(this._refreshTokenKey, value);
    } else {
      localStorage.removeItem(this._refreshTokenKey);
    }
  }

  public get deviceId(): string | null | undefined {
    return localStorage.getItem(this._deviceIdKey);
  }

  private set deviceId(value: string | null | undefined) {
    if (value) {
      localStorage.setItem(this._deviceIdKey, value);
    } else {
      localStorage.removeItem(this._deviceIdKey);
    }
  }

  public async performTokenRefreshAndScheduleRefresh(): Promise<ServerConnectionStateEvent> {
    const status = await this.performTokenRefresh();

    if (status.state !== ServerConnectionState.Unauthorized) {
      this.startScheduleRefresh();
    }

    return status;
  }

  private async performTokenRefresh(): Promise<ServerConnectionStateEvent> {
    const accessToken = this.accessToken;
    const refreshToken = this.refreshToken;
    const deviceId = this.deviceId;
    if (!accessToken || !refreshToken|| !deviceId) {
      return new ServerConnectionStateEvent(ServerConnectionState.Unauthorized, 'No Access Token, Refresh Token, or Device Id');
    }

    const result = await this._userClient.request('RefreshUser',
     client => client.refresh(new RefreshUserCommand({
       accessToken: accessToken.value,
       refreshToken: refreshToken,
       deviceId: deviceId,
     })));

    const refreshResponse = result.result ?? this.handleAuthError(result.err);

    if (refreshResponse instanceof RefreshUserResponse) {
      const stringAccessToken = refreshResponse?.accessToken;

      this.accessToken = stringAccessToken ? new JwtToken(stringAccessToken) : null;
      this.refreshToken = refreshResponse?.refreshToken;
      this.rolesRepository.roles = refreshResponse?.roles ?? [];

      if (refreshResponse) {
        this.deviceId = refreshResponse.deviceId;
      }

      if (!this.accessToken || !this.refreshToken) {
        return new ServerConnectionStateEvent(ServerConnectionState.Unauthorized, 'Missing Access or Refresh Token');
      }

      if (refreshResponse?.passwordResetRequired ?? false) {
        await this._router.navigate([PageRoutes.ResetPassword]);
        return new ServerConnectionStateEvent(ServerConnectionState.AwaitingPasswordReset, 'Password Reset Required');
      }

      return new ServerConnectionStateEvent(ServerConnectionState.Connected, 'Token Refreshed')
    }

    return new ServerConnectionStateEvent(refreshResponse, 'Failed to refresh token');
  }

  private startScheduleRefresh(): void {
    this.cancelScheduledRefresh();
    this.scheduleRefresh();
  }

  private scheduleRefresh(): void {
    const accessTokenExpiration = this.accessToken?.expires;
    const connectionStatus = this.serverConnectionStatus;

    if (connectionStatus === ServerConnectionState.Unauthorized) {
      return;
    }

    const connected = connectionStatus === ServerConnectionState.Connected ||
      connectionStatus === ServerConnectionState.AwaitingPasswordReset;

    // the number of seconds to try to refresh the token before it *actually* expires
    const timeout = accessTokenExpiration && connected
      ? accessTokenExpiration.getTime() - Date.now() - (30 * 1000)
      : 5 * 1000;

    // @ts-ignore
    this._refreshScheduleTimeout = setTimeout(async () => {
      const result = await this.performTokenRefresh()
      this.setServerConnectionStatus(result.state, result.reason);

      if (this.serverConnectionStatus === ServerConnectionState.Unauthorized) {
        return;
      }

      this.scheduleRefresh();
    }, timeout);

  }

  private cancelScheduledRefresh(): void {
    clearTimeout(this._refreshScheduleTimeout);
  }

  private handleAuthError(err: any): ServerConnectionState {
    if ((err instanceof ApiException || err instanceof ProblemDetails)) {
      if (err.status === 401 || err.status === 403) {
        this._toastService.error('Failed to refresh token', ServerConnectionState.Unauthorized)
        return ServerConnectionState.Unauthorized;
      }
      else {
        this._toastService.logServerError(err, 'Failed to refresh token')
        return ServerConnectionState.Disconnected;
      }
    }

    this._toastService.logServerError(err, 'Failed to refresh token');
    return ServerConnectionState.Disconnected;
  }
}
