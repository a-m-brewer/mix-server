import {Component, Input} from '@angular/core';
import {MatProgressSpinner} from "@angular/material/progress-spinner";

import {MatIcon} from "@angular/material/icon";

@Component({
    selector: 'app-loading-fab-icon',
    imports: [
    MatProgressSpinner,
    MatIcon
],
    templateUrl: './loading-fab-icon.component.html',
    styleUrl: './loading-fab-icon.component.scss'
})
export class LoadingFabIconComponent {
  @Input()
  public loading: boolean = false;

  @Input()
  public diameter: number = 24;
}
