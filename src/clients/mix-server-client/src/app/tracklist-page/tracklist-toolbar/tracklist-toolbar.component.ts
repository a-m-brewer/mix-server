import { Component } from '@angular/core';
import {MatButton} from "@angular/material/button";
import {TracklistFormService} from "../../services/tracklist/tracklist-form.service";
import {ImportTracklistFormComponent} from "./import-tracklist-form/import-tracklist-form.component";
import {SaveTracklistFormComponent} from "./save-tracklist-form/save-tracklist-form.component";
import {NgIf} from "@angular/common";

@Component({
  selector: 'app-tracklist-toolbar',
  standalone: true,
  imports: [
    MatButton,
    ImportTracklistFormComponent,
    SaveTracklistFormComponent,
    NgIf
  ],
  templateUrl: './tracklist-toolbar.component.html',
  styleUrl: './tracklist-toolbar.component.scss'
})
export class TracklistToolbarComponent {
  constructor(public tracklistFormService: TracklistFormService) {
  }
}
