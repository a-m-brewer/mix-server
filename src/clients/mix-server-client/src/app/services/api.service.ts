import {Injectable} from '@angular/core';
import {ToastService} from "./toasts/toast-service";
import {LoadingRepositoryService} from "./repositories/loading-repository.service";
import {LoadingAction} from "./repositories/models/loading-node-status";
import {firstValueFrom, Observable} from "rxjs";

@Injectable({
  providedIn: 'root'
})
export class ApiService<TClient> {

  constructor(private _client: TClient,
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
