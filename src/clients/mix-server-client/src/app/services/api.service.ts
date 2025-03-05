import {Injectable, InjectionToken, Injector} from '@angular/core';
import {ToastService} from "./toasts/toast-service";
import {LoadingRepositoryService} from "./repositories/loading-repository.service";
import {firstValueFrom, Observable} from "rxjs";
import {
  DeviceClient, NodeClient, NodeManagementClient,
  ProblemDetails,
  QueueClient,
  SessionClient, TracklistClient,
  TranscodeClient, UserClient
} from "../generated-clients/mix-server-clients";

export class ApiResult<T> {
  constructor(public err?: any,
              public result?: T) {
  }

  public success(action: (result: T) => void): ApiResult<T> {
    if (this.result) {
      action(this.result);
    }

    return this;
  }

  public error(action: (err: any) => void): ApiResult<T> {
    if (this.err) {
      action(this.err);
    }

    return this;
  }
}

export abstract class ApiService<TClient> {

  protected constructor(private _client: TClient,
                        private _loadingRepository: LoadingRepositoryService,
                        private _toastService: ToastService) {
  }

  public async request<TResponse>(action: string | Array<string>,
                                  call: (client: TClient) => Observable<TResponse>,
                                  message?: string,
                                  validStatusCodes: Array<number> = []): Promise<ApiResult<TResponse>> {
    if (Array.isArray(action)) {
      this._loadingRepository.startLoadingIds(action);
    }
    else {
      this._loadingRepository.startLoading(action);
    }

    try {
      return new ApiResult<TResponse>(undefined, await firstValueFrom(call(this._client)));
    } catch (err) {
      const statusCode = (err as ProblemDetails)?.status ?? 0;
      if (!validStatusCodes.includes(statusCode)) {
        this._toastService.logServerError(err, message ?? `Error calling ${action}`);
      }

      return new ApiResult(err);
    } finally {
      if (Array.isArray(action)) {
        this._loadingRepository.stopLoadingItems(action);
      }
      else {
        this._loadingRepository.stopLoading(action);
      }
    }
  }
}

@Injectable({
  providedIn: 'root'
})
export class DeviceApiService extends ApiService<DeviceClient> {
  constructor(_client: DeviceClient,
              _toastService: ToastService,
              _loadingRepository: LoadingRepositoryService) {
    super(_client, _loadingRepository, _toastService);
  }
}

@Injectable({
  providedIn: 'root'
})
export class NodeApiService extends ApiService<NodeClient> {
  constructor(_client: NodeClient,
              _toastService: ToastService,
              _loadingRepository: LoadingRepositoryService) {
    super(_client, _loadingRepository, _toastService);
  }
}

@Injectable({
  providedIn: 'root'
})
export class NodeManagementApiService extends ApiService<NodeManagementClient> {
  constructor(_client: NodeManagementClient,
              _toastService: ToastService,
              _loadingRepository: LoadingRepositoryService) {
    super(_client, _loadingRepository, _toastService);
  }
}

@Injectable({
    providedIn: 'root'
})
export class SessionApiService extends ApiService<SessionClient> {
    constructor(_client: SessionClient,
                _toastService: ToastService,
                _loadingRepository: LoadingRepositoryService) {
        super(_client, _loadingRepository, _toastService);
    }
}

@Injectable({
  providedIn: 'root'
})
export class TracklistApiService extends ApiService<TracklistClient> {
  constructor(_client: TracklistClient,
              _toastService: ToastService,
              _loadingRepository: LoadingRepositoryService) {
    super(_client, _loadingRepository, _toastService);
  }
}

@Injectable({
    providedIn: 'root'
})
export class QueueApiService extends ApiService<QueueClient> {
    constructor(_client: QueueClient,
                _toastService: ToastService,
                _loadingRepository: LoadingRepositoryService) {
        super(_client, _loadingRepository, _toastService);
    }
}

@Injectable({
  providedIn: 'root'
})
export class TranscodeApiService extends ApiService<TranscodeClient> {
  constructor(_client: TranscodeClient,
              _toastService: ToastService,
              _loadingRepository: LoadingRepositoryService) {
    super(_client, _loadingRepository, _toastService);
  }
}

@Injectable({
  providedIn: 'root'
})
export class UserApiService extends ApiService<UserClient> {
  constructor(_client: UserClient,
              _toastService: ToastService,
              _loadingRepository: LoadingRepositoryService) {
    super(_client, _loadingRepository, _toastService);
  }
}
