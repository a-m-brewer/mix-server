import { Component } from '@angular/core';
import {MatButton} from "@angular/material/button";
import {TracklistFormService} from "../../../services/tracklist/tracklist-form.service";

@Component({
  selector: 'app-save-tracklist-form',
  standalone: true,
  imports: [
    MatButton
  ],
  templateUrl: './save-tracklist-form.component.html',
  styleUrl: './save-tracklist-form.component.scss'
})
export class SaveTracklistFormComponent {
  constructor(private _tracklistFormService: TracklistFormService) {
  }

  public async saveTracklist(): Promise<void> {
    await this._tracklistFormService.saveTracklist();
  }
}
