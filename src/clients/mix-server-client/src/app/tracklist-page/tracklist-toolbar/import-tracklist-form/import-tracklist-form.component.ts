import { Component } from '@angular/core';
import {FormBuilder, FormControl, FormGroup, ReactiveFormsModule} from "@angular/forms";
import {MatFormField} from "@angular/material/form-field";
import {MatInput} from "@angular/material/input";
import {MatButton} from "@angular/material/button";
import {TracklistFormService} from "../../../services/tracklist/tracklist-form.service";

interface ImportTracklistFormGroup {
  file: FormControl<File | null | undefined>;
}

@Component({
  selector: 'app-import-tracklist-form',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatButton
  ],
  templateUrl: './import-tracklist-form.component.html',
  styleUrl: './import-tracklist-form.component.scss'
})
export class ImportTracklistFormComponent {

  public fileControl: FormControl<File | null | undefined>;
  public form: FormGroup<ImportTracklistFormGroup>;

  constructor(_fb: FormBuilder,
              private _formService: TracklistFormService) {
    this.fileControl = _fb.nonNullable.control<File | null | undefined>(null);
    this.form = _fb.group<ImportTracklistFormGroup>({
      file: this.fileControl
    });
  }

  public async onFileChange(e: Event): Promise<void> {
    const input = e.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      await this._formService.importTracklistFile(input.files[0]);
    }
  }
}
