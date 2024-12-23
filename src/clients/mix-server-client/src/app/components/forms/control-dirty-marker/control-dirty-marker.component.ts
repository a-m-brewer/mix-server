import {Component, Input} from '@angular/core';
import {NgIf} from "@angular/common";
import {AbstractControl} from "@angular/forms";
import {MatTooltip} from "@angular/material/tooltip";

@Component({
  selector: 'app-control-dirty-marker',
  standalone: true,
  imports: [
    NgIf,
    MatTooltip
  ],
  templateUrl: './control-dirty-marker.component.html',
  styleUrl: './control-dirty-marker.component.scss'
})
export class ControlDirtyMarkerComponent {
  @Input() control!: AbstractControl;
}
