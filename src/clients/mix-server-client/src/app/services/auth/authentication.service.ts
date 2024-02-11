import {Injectable} from '@angular/core';
import {Router} from "@angular/router";
import {PageRoutes} from "../../page-routes.enum";
import {ApiException, UserClient} from "../../generated-clients/mix-server-clients";
import {BehaviorSubject, distinctUntilChanged, firstValueFrom, map, Observable} from "rxjs";
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

@Injectable({
  providedIn: 'root'
})
export class AuthenticationService {
  private _connectionStatusBehaviourSubject$ = new BehaviorSubject<ServerConnectionState>(ServerConnectionState.Initializing);
  private _jwtTokenBehaviour$ = new BehaviorSubject<JwtToken | null>(null);

  private _refreshScheduleTimeout: number | undefined;
  private _accessTokenKey: string = 'accessToken';
  private _refreshTokenKey: string = 'refreshToken';
  private _deviceIdKey: string = 'deviceId';

  constructor(private rolesRepository: RoleRepositoryService,
              private _router: Router,
              private _toastService: ToastService,
              private _userClient: UserClient) {
    this.unauthorized$.subscribe(unauthorized => {
      if (unauthorized) {
        this._router.navigate([PageRoutes.Login])
          .catch((err) => this._toastService.logServerError(err, 'Failed to navigate'))
          .then();
      }
    });
  }

  public get serverConnectionStatus(): ServerConnectionState {
    return this._connectionStatusBehaviourSubject$.getValue();
  }

  public get serverConnectionStatus$(): Observable<ServerConnectionState> {
    return this._connectionStatusBehaviourSubject$.asObservable();
  }

  public get connected$(): Observable<boolean> {
    return this._connectionStatusBehaviourSubject$
      .pipe(map(s => s === ServerConnectionState.Connected))
      .pipe(distinctUntilChanged());
  }

  private get unauthorized$(): Observable<boolean> {
    return this._connectionStatusBehaviourSubject$
      .pipe(map(s => s === ServerConnectionState.Unauthorized))
      .pipe(distinctUntilChanged());
  }

  public set serverConnectionStatus(next: ServerConnectionState) {
    const current = this.serverConnectionStatus;
    if (next === current) {
      return;
    }

    if (next === ServerConnectionState.Disconnected && current === ServerConnectionState.Unauthorized) {
      return;
    }

    this._connectionStatusBehaviourSubject$.next(next);
  }

  public async initialize(): Promise<ServerConnectionState> {
    this.serverConnectionStatus = await this.performTokenRefresh();

    if (this.serverConnectionStatus === ServerConnectionState.Unauthorized) {
      return this.serverConnectionStatus;
    }

    this.startScheduleRefresh();

    return this.serverConnectionStatus;
  }

  public async login(username: string, password: string): Promise<void> {
    const deviceId = this.deviceId ?? undefined;

    const response = await firstValueFrom(this._userClient.login(new LoginUserCommand({
      username,
      password,
      deviceId
    }))).catch(err => {
      return this.handleAuthError(err);
    });

    if (response instanceof LoginCommandResponse) {
      this.accessToken = new JwtToken(response.accessToken);
      this.refreshToken = response.refreshToken;
      this.deviceId = response.deviceId;
      this.rolesRepository.roles = response.roles;

      if (response.passwordResetRequired) {
        this.serverConnectionStatus = ServerConnectionState.AwaitingPasswordReset;
        await this._router.navigate([PageRoutes.ResetPassword], {
          state: {
            loginPassword: password
          }
        });
      } else {
        this.serverConnectionStatus = ServerConnectionState.Connected;
        await this._router.navigate([PageRoutes.Files]);
      }

      this.startScheduleRefresh();

      return;
    }

    this.serverConnectionStatus = response;
  }

  public async resetPassword(currentPassword: string,
                             newPassword: string,
                             newPasswordConfirmation: string): Promise<boolean> {
    const success = await firstValueFrom(this._userClient.resetPassword(new ResetPasswordCommand({
      currentPassword,
      newPassword,
      newPasswordConfirmation
    })))
      .then(() => {
        return true;
      })
      .catch(() => {
        return false;
      });

    if (success) {
      this.serverConnectionStatus = ServerConnectionState.Connected;
      await this._router.navigate([PageRoutes.Files]);
    }

    return success;
  }

  public logout(): void {
    this.cancelScheduledRefresh();
    this.accessToken = null;
    this.refreshToken = null;
    this.serverConnectionStatus = ServerConnectionState.Unauthorized;
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

  public async performTokenRefreshAndScheduleRefresh(): Promise<ServerConnectionState> {
    const status = await this.performTokenRefresh();

    if (status !== ServerConnectionState.Unauthorized) {
      this.startScheduleRefresh();
    }

    return status;
  }

  private async performTokenRefresh(): Promise<ServerConnectionState> {
    if (!this.accessToken || !this.refreshToken || !this.deviceId) {
      return ServerConnectionState.Unauthorized;
    }

    const refreshResponse = await firstValueFrom(this._userClient.refresh(new RefreshUserCommand({
      accessToken: this.accessToken.value,
      refreshToken: this.refreshToken,
      deviceId: this.deviceId,
    }))).catch(err => {
      return this.handleAuthError(err);
    });

    if (refreshResponse instanceof RefreshUserResponse) {
      const stringAccessToken = refreshResponse?.accessToken;

      this.accessToken = stringAccessToken ? new JwtToken(stringAccessToken) : null;
      this.refreshToken = refreshResponse?.refreshToken;
      this.rolesRepository.roles = refreshResponse?.roles ?? [];

      if (refreshResponse) {
        this.deviceId = refreshResponse.deviceId;
      }

      if (!this.accessToken || !this.refreshToken) {
        return ServerConnectionState.Unauthorized;
      }

      if (refreshResponse?.passwordResetRequired ?? false) {
        await this._router.navigate([PageRoutes.ResetPassword]);
        return ServerConnectionState.AwaitingPasswordReset;
      }

      return ServerConnectionState.Connected;
    }

    return refreshResponse
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

    this._refreshScheduleTimeout = setTimeout(async () => {
      this.serverConnectionStatus = await this.performTokenRefresh();

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
