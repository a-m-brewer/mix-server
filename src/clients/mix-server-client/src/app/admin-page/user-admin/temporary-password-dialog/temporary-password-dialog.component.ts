import {Component, Inject} from '@angular/core';
import {FormsModule} from "@angular/forms";
import {MatButtonModule} from "@angular/material/button";
import {MatCheckboxModule} from "@angular/material/checkbox";
import {
  MAT_DIALOG_DATA,
  MatDialogActions,
  MatDialogClose,
  MatDialogContent,
  MatDialogTitle
} from "@angular/material/dialog";
import {MatFormFieldModule} from "@angular/material/form-field";
import {MatInputModule} from "@angular/material/input";

import {TemporaryPasswordData} from "./temporary-password-data";
import {CopyTextComponent} from "../../../components/controls/copy-text/copy-text.component";

@Component({
    selector: 'app-temporary-password-dialog',
    imports: [
    FormsModule,
    MatButtonModule,
    MatCheckboxModule,
    MatDialogActions,
    MatDialogContent,
    MatDialogTitle,
    MatFormFieldModule,
    MatInputModule,
    MatDialogClose,
    CopyTextComponent
],
    templateUrl: './temporary-password-dialog.component.html',
    styleUrl: './temporary-password-dialog.component.scss'
})
export class TemporaryPasswordDialogComponent {
  constructor(@Inject(MAT_DIALOG_DATA) public data: TemporaryPasswordData) {
  }
}
