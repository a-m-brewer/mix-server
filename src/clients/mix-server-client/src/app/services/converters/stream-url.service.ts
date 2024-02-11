import {Inject, Injectable} from '@angular/core';
import {MIXSERVER_BASE_URL} from "../../generated-clients/mix-server-clients";
import {AuthenticationService} from "../auth/authentication.service";

@Injectable({
  providedIn: 'root'
})
export class StreamUrlService {

  constructor(private _authenticationService: AuthenticationService,
              @Inject(MIXSERVER_BASE_URL) private _baseUrl: string) {
  }

  public getStreamUrl(playbackSessionId: string): string {
    const accessToken = this._authenticationService.accessToken?.value;
    const accessTokenQuery = accessToken ? `?access_token=${accessToken}` : '';

    return `${this._baseUrl}/api/stream/${playbackSessionId}` + accessTokenQuery
  }
}
