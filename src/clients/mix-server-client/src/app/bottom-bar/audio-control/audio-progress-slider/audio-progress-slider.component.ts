import {Component, OnDestroy, OnInit} from '@angular/core';
import {DurationDisplayComponent} from "../duration-display/duration-display.component";
import {MatSliderDragEvent, MatSliderModule} from "@angular/material/slider";
import {AudioPlayerService} from "../../../services/audio-player/audio-player.service";
import {FormsModule} from "@angular/forms";
import {AsyncPipe, NgClass, NgStyle} from "@angular/common";
import {Subject, takeUntil} from "rxjs";

export interface SliderMarker {
  positionSeconds: number;
  tooltip: string;
}

interface SliderMarkerDetails extends SliderMarker {
  width: number;
  positionPercentage: number;
}

@Component({
  selector: 'app-audio-progress-slider',
  standalone: true,
  imports: [
    DurationDisplayComponent,
    MatSliderModule,
    FormsModule,
    AsyncPipe,
    NgClass,
    NgStyle,
  ],
  templateUrl: './audio-progress-slider.component.html',
  styleUrl: './audio-progress-slider.component.scss'
})
export class AudioProgressSliderComponent implements OnInit, OnDestroy {
  private _unsubscribe$ = new Subject();
  private _currentTime: number = 0;

  constructor(public audioPlayer: AudioPlayerService) {
  }

  public duration: number = 0;
  public markers: SliderMarkerDetails[] = [];
  public hoverMarkerIndex: number | null = null;
  public thumbMarkerIndex: number | null = null;
  public percentageThroughCurrentMarker: number = 0;

  public ngOnInit(): void {
    // This is very important to avoid NG0100: Expression has changed after it was checked
    // The audio.currentTime can not be binded directly to the slider value as it changes outside of Angulars change detection
    this.audioPlayer.sampleCurrentTime$(500, false)
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(currentTime => {
        this._currentTime = currentTime;
        this.calculateThumbMarkerIndex();
      });

    this.audioPlayer.duration$
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(duration => {
        this.duration = duration;
        this.updateMarkers([
          {positionSeconds: 0, tooltip: 'Marker 0'},
          {positionSeconds: 10 * 72, tooltip: 'Marker 10'},
          {positionSeconds: 50 * 72, tooltip: 'Marker 50'},
          {positionSeconds: 90 * 72, tooltip: 'Marker 90'},
        ]);
      });
  }

  public ngOnDestroy(): void {
    this._unsubscribe$.next(null);
    this._unsubscribe$.complete();
  }

  public get currentTime(): number {
    return this._currentTime;
  }

  public set currentTime(value: number) {
    this.audioPlayer.currentTime = value;
  }

  public get thumbInHoverMarker(): boolean {
    return!!this.hoverMarkerIndex && !!this.thumbMarkerIndex && this.hoverMarkerIndex === this.thumbMarkerIndex
  }

  public sliderDragEnded(event: MatSliderDragEvent) {
    this.audioPlayer.seek(event.value);
  }

  private updateMarkers(nextMarkers: SliderMarker[]) {
    const nextMarkerDetails: SliderMarkerDetails[] = [];

    const percentages = nextMarkers.map(marker => (marker.positionSeconds / this.duration) * 100);

    for (let i = 0; i < nextMarkers.length; i++) {
      const currentPosition = percentages[i];
      const nextMarkerPosition = i < percentages.length - 1 ? percentages[i + 1] : 100;

      const nextMarkerDetail = {
        ...nextMarkers[i],
        width: nextMarkerPosition - currentPosition,
        positionPercentage: currentPosition,
      }

      nextMarkerDetails.push(nextMarkerDetail);
    }

    this.markers = nextMarkerDetails;
  }

  onSliderHover(e: MouseEvent) {
    const sliderContainer = e.currentTarget as HTMLElement;
    const rect = sliderContainer.getBoundingClientRect();
    const positionRaw = ((e.clientX - rect.left) / rect.width) * 100;
    const position = Math.min(100, Math.max(0, positionRaw));

    this.hoverMarkerIndex = this.calculateHoverMarkerIndex(position);
  }

  clearHover() {
    this.hoverMarkerIndex = null;
  }

  private calculateThumbMarkerIndex() {
    const position = (this.currentTime / this.duration) * 100;

    this.thumbMarkerIndex = this.calculateHoverMarkerIndex(position);
    this.percentageThroughCurrentMarker = this.thumbMarkerIndex !== null
      ? ((position - this.markers[this.thumbMarkerIndex].positionPercentage) / this.markers[this.thumbMarkerIndex].width) * 100
      : 0;
  }

  private calculateHoverMarkerIndex(position: number): number | null {
    for (let i = this.markers.length - 1; i >= 0; i--) {
      const marker = this.markers[i];
      if (marker.positionPercentage <= position) {
        return i;
      }
    }

    return null;
  }
}
