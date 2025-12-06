import { Component } from '@angular/core';
import {MatButtonModule} from "@angular/material/button";
import {MatCardModule} from "@angular/material/card";
import {MatFormFieldModule} from "@angular/material/form-field";
import {MatInputModule} from "@angular/material/input";
import {MatProgressBarModule} from "@angular/material/progress-bar";

import {FormBuilder, FormControl, FormGroup, ReactiveFormsModule, ValidationErrors, Validators} from "@angular/forms";
import Validation from "../../utils/validation";
import {AuthenticationService} from "../../services/auth/authentication.service";
import {Router} from "@angular/router";

interface ResetPasswordForm {
  currentPassword: FormControl<string>;
  newPassword: FormControl<string>;
  newPasswordConfirmation: FormControl<string>;
}

@Component({
    selector: 'app-reset-password',
    imports: [
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressBarModule,
    ReactiveFormsModule
],
    templateUrl: './reset-password.component.html',
    styleUrl: './reset-password.component.scss'
})
export class ResetPasswordComponent {
  public form: FormGroup<ResetPasswordForm>;
  public currentPasswordKey = 'currentPassword';
  public newPasswordKey = 'newPassword';
  public newPasswordConfirmationKey = 'newPasswordConfirmation';

  public loading: boolean = false;
  public loginPassword = '';

  constructor(private _authService: AuthenticationService,
              private _formBuilder: FormBuilder,
              _router: Router) {
    const currentNavigation = _router.currentNavigation();
    const state = currentNavigation?.extras.state as { loginPassword?: string | null };
    this.loginPassword = state?.loginPassword ?? '';

    this.form = this._formBuilder.nonNullable.group<ResetPasswordForm>({
      currentPassword: _formBuilder.nonNullable.control('', [
        Validation.requiredIfMissing(this.loginPassword)
      ]),
      newPassword: _formBuilder.nonNullable.control('', [
        Validators.required
      ]),
      newPasswordConfirmation: _formBuilder.nonNullable.control('', [
        Validators.required
      ])
    }, {
      validators: [
        Validation.match(this.newPasswordKey, this.newPasswordConfirmationKey)
      ]
    });
  }

  public onSubmit(): void {
    this.loading = true;

    const {
      currentPassword,
      newPassword,
      newPasswordConfirmation
    } = {...this.form.value};

    const foundCurrentPassword = this.loginPassword && this.loginPassword.trim() !== ''
      ? this.loginPassword
      : currentPassword;

    if (!foundCurrentPassword) {
      return;
    }

    this._authService
      .resetPassword(foundCurrentPassword, newPassword!, newPasswordConfirmation!)
      .finally(() => this.loading = false);
  }
}
