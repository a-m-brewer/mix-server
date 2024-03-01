import {Injectable} from "@angular/core";
import {ToastrService} from "ngx-toastr";
import {ApiException, ProblemDetails} from "../../generated-clients/mix-server-clients";
import {LoggingService} from "../logging.service";

@Injectable({
  providedIn: 'root'
})
export class ToastService {
  constructor(private _loggingService: LoggingService,
              private _toastService: ToastrService) {
  }

  public warning(message?: string, title?: string): void {
    this._toastService.warning(message, title);
  }

  public error(message?: string, title?: string): void {
    this._toastService.error(message, title);
  }

  public logServerError(err: any, message?: string) {
    const extraMessage = `${message}: ` ?? '';
    console.error(err);

    if (err instanceof ProblemDetails) {
      const problem = err as ProblemDetails;
      this._toastService.error(`${extraMessage}${problem.detail}`, `${problem.status}: ${problem.title}`);
      this._loggingService.error(`[${problem.status}: ${problem.title}] ${extraMessage}${problem.detail}`);
    }
    else if (err instanceof ApiException) {
      const title = err.status === 0 ? 'Network Error' : err.status.toString();
      this._toastService.error(`${extraMessage}${err.message}`, title);
      this._loggingService.error(`[${title}] ${extraMessage}${err.message}`);
    }
    else if (err instanceof Error) {
      this._toastService.error(`${extraMessage}${err.message}`, 'Error');
      this._loggingService.error(`[Error] ${extraMessage}${err.message}`);
    }
    else {
      this.error(`${extraMessage}check console output`, 'Unknown Error');
    }
  }
}
