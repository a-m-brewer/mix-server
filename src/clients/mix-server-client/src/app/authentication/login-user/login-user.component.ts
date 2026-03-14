import {Component, OnDestroy} from '@angular/core';
import {FormBuilder, FormControl, FormGroup, Validators} from "@angular/forms";
import {LoginUserForm} from "./login-user-form.interface";
import {Subject} from "rxjs";
import {AuthenticationService} from "../../services/auth/authentication.service";
import {Capacitor} from "@capacitor/core";
import {getStoredServerUrl, hasStoredServerUrl, setStoredServerUrl} from "../../api-url-getter";

@Component({
    selector: 'app-login-user',
    templateUrl: './login-user.component.html',
    styleUrls: ['./login-user.component.scss'],
    standalone: false
})
export class LoginUserComponent implements OnDestroy {
  private _unsubscribe$ = new Subject();
  private _initialServerUrl: string;

  public usernameKey = 'username';
  public passwordKey = 'password';

  public form: FormGroup<LoginUserForm>;
  public serverUrlControl: FormControl<string> | null = null;
  public loading: boolean = false;
  public isNative: boolean;

  constructor(private _authenticationService: AuthenticationService,
              private _formBuilder: FormBuilder) {
    this.isNative = Capacitor.isNativePlatform();
    this._initialServerUrl = getStoredServerUrl();

    this.form = this._formBuilder.nonNullable.group<LoginUserForm>({
      username: _formBuilder.nonNullable.control('', [
        Validators.required
      ]),
      password: _formBuilder.nonNullable.control('', [
        Validators.required
      ])
    });

    if (this.isNative) {
      this.serverUrlControl = _formBuilder.nonNullable.control(this._initialServerUrl, [
        Validators.required
      ]);
    }
  }

  public get canSubmitLogin(): boolean {
    if (!this.form.valid || this.loading) {
      return false;
    }

    if (this.isNative && this.serverUrlControl && !this.serverUrlControl.valid) {
      return false;
    }

    return true;
  }

  public ngOnDestroy(): void {
    this._unsubscribe$.next(null);
    this._unsubscribe$.complete();
  }

  public onSubmit(): void {
    this.loading = true;

    if (this.isNative && this.serverUrlControl) {
      const serverUrl = this.serverUrlControl.value.replace(/\/+$/, '');
      setStoredServerUrl(serverUrl);

      if (serverUrl !== this._initialServerUrl) {
        // Server URL changed — reload to re-initialize the MIXSERVER_BASE_URL injection token
        window.location.reload();
        return;
      }
    }

    const { username, password } = {...this.form.value};

    this._authenticationService
      .login(username!, password!)
      .finally(() => {
        this.loading = false;
      });
  }
}
