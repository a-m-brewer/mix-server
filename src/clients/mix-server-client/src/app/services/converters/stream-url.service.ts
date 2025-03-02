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
              @Inject(MIXSERVER_BASE_URL) private _baseUrl: string) {
  }

  public getStreamUrl(playbackSessionId: string, key: string, expires: number): string {
    const deviceId = this._authenticationService.deviceId ?? '';
    const encodedKey = encodeURIComponent(key);

    return `${this._baseUrl}/api/stream/${playbackSessionId}?key=${encodedKey}&expires=${expires}&deviceId=${deviceId}`;
  }
}
