import {Inject, Injectable} from '@angular/core';
import {MIXSERVER_BASE_URL, StreamClient} from "../../generated-clients/mix-server-clients";
import {AuthenticationService} from "../auth/authentication.service";
import {firstValueFrom} from "rxjs";
import {ToastService} from "../toasts/toast-service";

@Injectable({
  providedIn: 'root'
})
export class StreamUrlService {

  constructor(private _authenticationService: AuthenticationService,
              @Inject(MIXSERVER_BASE_URL) private _baseUrl: string,
              private _streamClient: StreamClient,
              private _toastService: ToastService) {
  }

  public async getStreamUrl(playbackSessionId: string): Promise<string | null> {
    let key: string;
    let expires: number;
    try {
      const response = await firstValueFrom(this._streamClient.generateStreamKey(playbackSessionId));
      key = response.key;
      expires = response.expires;
    } catch (err) {
      this._toastService.logServerError(err, 'Failed to generate stream key');
      return null;
    }

    const deviceId = this._authenticationService.deviceId ?? '';

    console.log(key, expires);
    return `${this._baseUrl}/api/stream/${playbackSessionId}?key=${key}&expires=${expires}&deviceId=${deviceId}`;
  }
}
