import {
  AfterViewChecked,
  AfterViewInit,
  ChangeDetectorRef,
  Component,
  OnDestroy,
  OnInit,
  QueryList,
  ViewChildren
} from '@angular/core';
import {AsyncPipe, NgClass, NgIf, NgStyle} from "@angular/common";
import {FormsModule} from "@angular/forms";
import {MatSlider, MatSliderDragEvent, MatSliderThumb} from "@angular/material/slider";
import {AudioPlayerService} from "../../../services/audio-player/audio-player.service";
import {BehaviorSubject, combineLatestWith, filter, Subject, takeUntil} from "rxjs";
import {MatTooltip} from "@angular/material/tooltip";

export interface SliderMarker {
  positionSeconds: number;
  tooltip: string;
}

interface SliderMarkerDetails extends SliderMarker {
  width: number;
  positionPercentage: number;
}

@Component({
  selector: 'app-audio-slider',
  standalone: true,
  imports: [
    AsyncPipe,
    FormsModule,
    MatSlider,
    MatSliderThumb,
    NgClass,
    NgStyle,
    MatTooltip,
    NgIf
  ],
  templateUrl: './audio-slider.component.html',
  styleUrl: './audio-slider.component.scss'
})
export class AudioSliderComponent implements OnInit, OnDestroy, AfterViewInit {
  private _unsubscribe$ = new Subject();
  private _currentTime: number = 0;

  private _hoverMarkerIndex$ = new BehaviorSubject<number>(-1);
  private _thumbMarkerIndex$ = new BehaviorSubject<number>(-1);

  private _thumbContainer: HTMLElement | null | undefined = null;
  private _thumb: HTMLElement | null | undefined = null;
  private _tooltips: MatTooltip[] = [];

  public duration: number = 0;
  public markers: SliderMarkerDetails[] = [];
  public hoverMarkerIndex: number = -1;
  public thumbMarkerIndex: number = -1;
  public percentageThroughCurrentMarker: number = 0;
  public percentageRemaining = 0;
  public thumbInHoverMarker: boolean = false;
  public isThumbActive: boolean = false;

  constructor(public audioPlayer: AudioPlayerService) {
  }

  @ViewChildren(MatTooltip) tooltipsQuery!: QueryList<MatTooltip>;

  public ngOnInit(): void {
    // This is very important to avoid NG0100: Expression has changed after it was checked
    // The audio.currentTime can not be binded directly to the slider value as it changes outside of Angulars change detection
    this.audioPlayer.currentTime$
      .pipe(takeUntil(this._unsubscribe$))
      .pipe(combineLatestWith(this.audioPlayer.duration$))
      .pipe(filter(([, duration]) => duration > 0))
      .subscribe(([currentTime, duration]) => {
        this._currentTime = currentTime;
        this.duration = duration;

        this.updateMarkers([
          {positionSeconds: 10 * 72, tooltip: 'Marker 10'},
          // {positionSeconds: 50 * 72, tooltip: 'Marker 50'},
          // {positionSeconds: 90 * 72, tooltip: 'Marker 90'},
        ]);
        this.calculateThumbMarkerIndex();
      });

    this._hoverMarkerIndex$
      .pipe(takeUntil(this._unsubscribe$))
      .pipe(filter(() => this.markers.length > 0))
      .pipe(combineLatestWith(this._thumbMarkerIndex$))
      .subscribe(([hoverMarkerIndex, thumbMarkerIndex]) => {
        this.hoverMarkerIndex = hoverMarkerIndex;
        this.thumbMarkerIndex = thumbMarkerIndex;

        this.thumbInHoverMarker = this.hoverMarkerIndex > -1 && this.thumbMarkerIndex > -1 && this.hoverMarkerIndex === this.thumbMarkerIndex

        if (this._thumb && this._thumbContainer) {
          const focus = document.querySelector('.mat-mdc-slider-focus-ripple') as HTMLElement | null | undefined;
          const active = document.querySelector('.mat-mdc-slider-active-ripple') as HTMLElement | null | undefined;

          if (this.thumbInHoverMarker) {
            this._thumbContainer.style.width = '64px';
            this._thumbContainer.style.height = '64px';
            this._thumbContainer.style.left = '-32px';

            this._thumb.style.width = '30px';
            this._thumb.style.height = '30px';

            if (active) {
              active.style.width = '64px';
              active.style.height = '64px';
              active.style.left = '0px';
              active.style.top = '0px';
            }

            if (focus) {
              focus.style.width = '64px';
              focus.style.height = '64px';
              focus.style.left = '0px';
              focus.style.top = '0px';
            }

          } else {
            this._thumbContainer.style.width = '48px';
            this._thumbContainer.style.height = '48px';
            this._thumbContainer.style.left = '-24px';

            if (active) {
              active.style.width = '48px';
              active.style.height = '48px';
              active.style.left = '0px';
              active.style.top = '0px';
            }

            if (focus) {
              focus.style.width = '48px';
              focus.style.height = '48px';
              focus.style.left = '0px';
              focus.style.top = '0px';
            }

            this._thumb.style.width = '20px';
            this._thumb.style.height = '20px';
          }
        }

        this._tooltips.forEach((tooltip, index) => {
          if (index === this.hoverMarkerIndex) {
            tooltip.show();
          } else {
            tooltip.hide();
          }
        });
      });
  }

  public ngOnDestroy(): void {
    this._unsubscribe$.next(null);
    this._unsubscribe$.complete();
  }

  public ngAfterViewInit(): void {
    this._thumbContainer = document.querySelector('.mat-mdc-slider-visual-thumb') as HTMLElement | null | undefined;
    this._thumb = document.querySelector('div.mdc-slider__thumb-knob') as HTMLElement | null | undefined;

    if (this._thumbContainer && this._thumb) {
      this._thumbContainer.style.zIndex = '2';
    }

    this._tooltips = this.tooltipsQuery.toArray();
  }

  public get currentTime(): number {
    return this._currentTime;
  }

  public set currentTime(value: number) {
    this.audioPlayer.currentTime = value;
  }

  public seekToMarker(positionSeconds: number) {
    this.currentTime = positionSeconds;
  }

  public sliderDragEnded(event: MatSliderDragEvent) {
    this.isThumbActive = false;
    this.audioPlayer.seek(event.value);
  }

  public sliderDragStarted() {
    this.isThumbActive = true; // Thumb is active
    this.updateHoverMarkerIndex();
  }

  public onTouchStart(event: TouchEvent) {
    this.sliderDragStarted();
    this.onSliderHover(event);
  }

  public onTouchMove(event: TouchEvent) {
    this.onSliderHover(event);
  }

  public onTouchEnd() {
    this.isThumbActive = false;
    this.sliderDragEnded({value: this._currentTime} as MatSliderDragEvent);
  }

  private   updateMarkers(nextMarkers: SliderMarker[]) {
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
    this.percentageRemaining = 100 - this.markers.reduce((acc, marker) => acc + marker.width, 0);
    this._tooltips = this.tooltipsQuery.toArray();
  }

  onSliderHover(e: MouseEvent | TouchEvent) {
    const sliderContainer = e.currentTarget as HTMLElement;
    const rect = sliderContainer.getBoundingClientRect();
    const clientX = (e instanceof TouchEvent) ? e.touches[0].clientX : e.clientX;
    const positionRaw = ((clientX - rect.left) / rect.width) * 100;
    const position = Math.min(100, Math.max(0, positionRaw));

    this._hoverMarkerIndex$.next(this.calculateHoverMarkerIndex(position));
  }

  clearHover() {
    if (!this.isThumbActive) {
      this._hoverMarkerIndex$.next(-1);
    }
  }

  private calculateThumbMarkerIndex() {
    const position = (this.currentTime / this.duration) * 100;

    const thumbIndex = this.calculateHoverMarkerIndex(position);

    if (this.thumbMarkerIndex !== thumbIndex) {
      this._thumbMarkerIndex$.next(thumbIndex);
    }

    this.percentageThroughCurrentMarker = this.thumbMarkerIndex !== -1
      ? ((position - this.markers[this.thumbMarkerIndex].positionPercentage) / this.markers[this.thumbMarkerIndex].width) * 100
      : 0;
  }

  private calculateHoverMarkerIndex(position: number): number {
    for (let i = this.markers.length - 1; i >= 0; i--) {
      const marker = this.markers[i];
      if (marker.positionPercentage <= position) {
        return i;
      }
    }

    return -1;
  }

  private updateHoverMarkerIndex() {
    // Update the hover marker index based on the current thumb position
    const position = (this.currentTime / this.duration) * 100;
    this._hoverMarkerIndex$.next(this.calculateHoverMarkerIndex(position));
  }
}
