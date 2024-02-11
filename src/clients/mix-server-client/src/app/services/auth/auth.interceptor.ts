import {HttpEvent, HttpHandler, HttpInterceptor, HttpRequest} from "@angular/common/http";
import {Injectable} from "@angular/core";
import {from, lastValueFrom, Observable} from "rxjs";
import {AuthenticationService} from "./authentication.service";

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  constructor(private _authenticationService: AuthenticationService) {
  }

  public intercept(request: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    return from(this.handle(request, next));
  }

  private async handle(req: HttpRequest<any>, next: HttpHandler): Promise<HttpEvent<any>> {
    const accessToken = this._authenticationService.accessToken?.value;
    if (accessToken) {
      req = req.clone({
        setHeaders: {
          Authorization: `Bearer ${accessToken}`
        }
      });
    }

    return await lastValueFrom(next.handle(req));
  }
}
