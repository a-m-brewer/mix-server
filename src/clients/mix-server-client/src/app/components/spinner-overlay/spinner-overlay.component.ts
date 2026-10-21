import {Component, Input, ChangeDetectionStrategy} from '@angular/core';

@Component({
    selector: 'app-spinner-overlay',
    templateUrl: './spinner-overlay.component.html',
    styleUrls: ['./spinner-overlay.component.scss'],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class SpinnerOverlayComponent {
  @Input()
  public reason?: string | null;
}
