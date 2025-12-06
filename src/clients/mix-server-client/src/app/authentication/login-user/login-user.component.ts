import {Component, OnDestroy, OnInit} from '@angular/core';
import {FormBuilder, FormGroup, Validators} from "@angular/forms";
import {LoginUserForm} from "./login-user-form.interface";
import {Subject} from "rxjs";
import {AuthenticationService} from "../../services/auth/authentication.service";
import {TitleService} from "../../services/title/title.service";

@Component({
    selector: 'app-login-user',
    templateUrl: './login-user.component.html',
    styleUrls: ['./login-user.component.scss'],
    standalone: false
})
export class LoginUserComponent implements OnDestroy {
  private _unsubscribe$ = new Subject();

  public usernameKey = 'username';
  public passwordKey = 'password';

  public form: FormGroup<LoginUserForm>;
  public loading: boolean = false;

  constructor(private _authenticationService: AuthenticationService,
              private _formBuilder: FormBuilder) {
    this.form = this._formBuilder.nonNullable.group<LoginUserForm>({
      username: _formBuilder.nonNullable.control('', [
        Validators.required
      ]),
      password: _formBuilder.nonNullable.control('', [
        Validators.required
      ])
    });
  }

  public ngOnDestroy(): void {
    this._unsubscribe$.next(null);
    this._unsubscribe$.complete();
  }

  public onSubmit(): void {
    this.loading = true;

    const { username, password } = {...this.form.value};

    this._authenticationService
      .login(username!, password!)
      .finally(() => {
        this.loading = false;
      });
  }
}
