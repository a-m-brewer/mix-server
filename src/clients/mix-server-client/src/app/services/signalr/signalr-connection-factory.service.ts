import {Injectable} from '@angular/core';
import {
  HubConnection,
  HubConnectionBuilder,
  IHttpConnectionOptions,
  IRetryPolicy,
  LogLevel,
  RetryContext
} from "@microsoft/signalr";
import {AuthenticationService} from "../auth/authentication.service";

@Injectable({
  providedIn: 'root'
})
export class SignalrConnectionFactoryService {
  private readonly _baseRetryMilliseconds: number = 100;
  private readonly _maxRetryMilliseconds: number = 1000;
  private readonly _maxRetryAttempts: number = 20;

  constructor(private _authenticationService: AuthenticationService) {
  }

  public buildConnection(callbackUrl: string): HubConnection {
    const connectionOptions = this.createConnectionOptions();
    const retryPolicy = this.createRetryPolicy();

    return new HubConnectionBuilder()
      .withUrl(callbackUrl, connectionOptions)
      .withAutomaticReconnect(retryPolicy)
      .configureLogging(LogLevel.None)
      .build();
  }

  private createConnectionOptions(): IHttpConnectionOptions {
    return {
      accessTokenFactory: () =>
        this._authenticationService.accessToken?.value ?? ''
    };
  }

  private createRetryPolicy(): IRetryPolicy {
    return {
      nextRetryDelayInMilliseconds: this.calculateRetryDelay.bind(this)
    };
  }

  public calculateRetryDelay(retryContext: RetryContext): number | null {
    if (retryContext.previousRetryCount >= this._maxRetryAttempts) {
      // 'null' tells SignalR to stop retrying the connection
      return null;
    }

    const calculatedDelay =
      this._baseRetryMilliseconds * (retryContext.previousRetryCount + 1);

    return Math.min(calculatedDelay, this._maxRetryMilliseconds);
  }
}
