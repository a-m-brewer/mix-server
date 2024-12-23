import { Component } from '@angular/core';
import {MatButton} from "@angular/material/button";
import {TracklistFormService} from "../../services/tracklist/tracklist-form.service";
import {ImportTracklistFormComponent} from "./import-tracklist-form/import-tracklist-form.component";

@Component({
  selector: 'app-tracklist-toolbar',
  standalone: true,
  imports: [
    MatButton,
    ImportTracklistFormComponent
  ],
  templateUrl: './tracklist-toolbar.component.html',
  styleUrl: './tracklist-toolbar.component.scss'
})
export class TracklistToolbarComponent {
  constructor(private _tracklistFormService: TracklistFormService) {
  }

  public onImport(): void {
  }
}
