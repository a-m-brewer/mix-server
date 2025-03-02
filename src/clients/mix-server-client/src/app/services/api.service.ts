import {Injectable, InjectionToken, Injector} from '@angular/core';
import {ToastService} from "./toasts/toast-service";
import {LoadingRepositoryService} from "./repositories/loading-repository.service";
import {firstValueFrom, Observable} from "rxjs";
import {QueueClient, SessionClient, TranscodeClient} from "../generated-clients/mix-server-clients";

export abstract class ApiService<TClient> {

  protected constructor(private _client: TClient,
                        private _loadingRepository: LoadingRepositoryService,
                        private _toastService: ToastService) {
  }

  public async request<TResponse>(action: string,
                                  call: (client: TClient) => Observable<TResponse>,
                                  message?: string): Promise<TResponse> {
    this._loadingRepository.startLoading(action);
    try {
      return await firstValueFrom(call(this._client));
    } catch (err) {
      this._toastService.logServerError(err, message ?? `Error calling ${action}`);
      throw err;
    } finally {
      this._loadingRepository.stopLoading(action);
    }
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
