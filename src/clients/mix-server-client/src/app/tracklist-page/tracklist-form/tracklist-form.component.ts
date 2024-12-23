import { Component } from '@angular/core';
import {TracklistFormService} from "../../services/tracklist/tracklist-form.service";
import {FormArray, ReactiveFormsModule} from "@angular/forms";
import {
  MatList,
  MatListItem,
  MatListItemLine, MatListItemMeta,
  MatListItemTitle,
  MatListSubheaderCssMatStyler
} from "@angular/material/list";
import {MatDivider} from "@angular/material/divider";
import {NgIf} from "@angular/common";
import {MatAnchor} from "@angular/material/button";
import {ControlDirtyMarkerComponent} from "../../components/forms/control-dirty-marker/control-dirty-marker.component";

@Component({
  selector: 'app-tracklist-form',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatList,
    MatListItem,
    MatListItemTitle,
    MatListItemLine,
    MatDivider,
    NgIf,
    MatListSubheaderCssMatStyler,
    MatListItemMeta,
    MatAnchor,
    ControlDirtyMarkerComponent
  ],
  templateUrl: './tracklist-form.component.html',
  styleUrl: './tracklist-form.component.scss'
})
export class TracklistFormComponent {
  constructor(public formService: TracklistFormService) {
  }

  protected readonly FormArray = FormArray;
}
