import {Component, Input, OnChanges, SimpleChanges} from '@angular/core';
import {ComponentChanges} from "../../../utils/component-changes";

@Component({
  selector: 'app-duration-display',
  templateUrl: './duration-display.component.html',
  styleUrls: ['./duration-display.component.scss']
})
export class DurationDisplayComponent implements OnChanges {
  @Input()
  public duration: number = 0;

  public displayDuration: string = '';

  public ngOnChanges(changes: ComponentChanges<DurationDisplayComponent>): void {
    if (changes.duration) {
      this.displayDuration = this.generateStringValue(changes.duration.currentValue);
    }
  }

  private generateStringValue(durationInSeconds: number | undefined | null): string {
    if (!durationInSeconds) {
      return '00:00:00';
    }

    const totalMinutes = Math.floor(durationInSeconds / 60);

    const seconds = Math.floor(durationInSeconds % 60).toString().padStart(2, '0');
    const hours = (Math.floor(totalMinutes / 60)).toString().padStart(2, '0');
    const minutes = (totalMinutes % 60).toString().padStart(2, '0');

    return `${hours}:${minutes}:${seconds}`;
  }
}
