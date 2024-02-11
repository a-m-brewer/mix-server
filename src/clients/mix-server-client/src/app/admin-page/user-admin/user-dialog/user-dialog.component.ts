import {Component, Inject, OnInit} from '@angular/core';
import {MatButtonModule} from "@angular/material/button";
import {
  MAT_DIALOG_DATA,
  MatDialogActions,
  MatDialogClose,
  MatDialogContent,
  MatDialogRef,
  MatDialogTitle
} from "@angular/material/dialog";
import {FormBuilder, FormControl, FormGroup, ReactiveFormsModule, Validators} from "@angular/forms";
import {MatFormFieldModule} from "@angular/material/form-field";
import {MatInputModule} from "@angular/material/input";
import {NgIf} from "@angular/common";
import {MatCheckboxModule} from "@angular/material/checkbox";
import {UserDialogData} from "./user-dialog-data";

interface AddUserForm {
  username: FormControl<string>;
  isAdmin: FormControl<boolean>
}

export class AddUserDialogResponse {
  constructor(public username: string,
              public isAdmin: boolean) {
  }
}

@Component({
  selector: 'app-user-dialog',
  standalone: true,
  imports: [
    MatButtonModule,
    MatDialogActions,
    MatDialogContent,
    MatDialogTitle,
    MatDialogClose,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    NgIf,
    MatCheckboxModule
  ],
  templateUrl: './user-dialog.component.html',
  styleUrl: './user-dialog.component.scss'
})
export class UserDialogComponent {
  public usernameKey = 'username';
  public isAdminKey = 'isAdmin';

  public form: FormGroup<AddUserForm>;

  constructor(@Inject(MAT_DIALOG_DATA) public data: UserDialogData,
              private _dialogRef: MatDialogRef<UserDialogComponent>,
              private _formBuilder: FormBuilder) {
    this.form = this._formBuilder.nonNullable.group<AddUserForm>({
      username: _formBuilder.nonNullable.control(data?.username ?? '', [
        Validators.required
      ]),
      isAdmin: _formBuilder.nonNullable.control(data?.isAdmin ?? false)
    });
  }

  public onSubmit(): void {
    const { username, isAdmin } = {...this.form.value};

    if (!username || isAdmin == undefined) {
      this._dialogRef.close(null);
      return
    }

    this._dialogRef.close(new AddUserDialogResponse(username, isAdmin));
  }
}
