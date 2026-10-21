import {Injectable} from "@angular/core";
import {MatSnackBar} from "@angular/material/snack-bar";
import {ApiException, ProblemDetails} from "../../generated-clients/mix-server-clients";

@Injectable({
  providedIn: 'root'
})
export class ToastService {
  constructor(private _snackBar: MatSnackBar) {
  }

  public warning(message?: string, title?: string): void {
    this.open(message, title, 'toast-warning');
  }

  public error(message?: string, title?: string): void {
    this.open(message, title, 'toast-error');
  }

  private open(message?: string, title?: string, panelClass?: string): void {
    this._snackBar.open(message ?? '', title, {
      duration: 5_000,
      panelClass
    });
  }

  public logServerError(err: any, message?: string) {
    const extraMessage = message ? `${message}: ` : '';
    console.error(err);

    if (err instanceof ProblemDetails) {
      const problem = err as ProblemDetails;
      this.error(`${extraMessage}${problem.detail}`, `${problem.status}: ${problem.title}`);
    }
    else if (err instanceof ApiException) {
      const title = err.status === 0 ? 'Network Error' : err.status.toString();
      this.error(`${extraMessage}${err.message}`, title);
    }
    else if (err instanceof Error) {
      this.error(`${extraMessage}${err.message}`, 'Error');
    }
    else {
      this.error(`${extraMessage}check console output`, 'Unknown Error');
    }
  }
}
