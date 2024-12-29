import {Component, Input} from '@angular/core';
import {FormBuilder, FormControl, FormGroup, ReactiveFormsModule} from "@angular/forms";
import {MatButton} from "@angular/material/button";
import {TracklistFormService} from "../../../services/tracklist/tracklist-form.service";
import {TracklistForm} from "../../../services/tracklist/models/tracklist-form.interface";

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
              public formService: TracklistFormService) {
    this.fileControl = _fb.nonNullable.control<File | null | undefined>(null);
    this.form = _fb.group<ImportTracklistFormGroup>({
      file: this.fileControl
    });
  }

  @Input()
  public tracklistForm?: FormGroup<TracklistForm>;

  public async onFileChange(e: Event): Promise<void> {
    const input = e.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      await this.formService.importTracklistFile(input.files[0]);
    }
  }
}
