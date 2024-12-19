import {AfterViewInit, Component, ElementRef, OnDestroy, OnInit, ViewChild} from '@angular/core';
import {DurationDisplayComponent} from "../duration-display/duration-display.component";
import {MatSlider, MatSliderDragEvent, MatSliderModule} from "@angular/material/slider";
import {AudioPlayerService} from "../../../services/audio-player/audio-player.service";
import {FormsModule} from "@angular/forms";
import {AsyncPipe, NgForOf} from "@angular/common";
import {Subject, takeUntil} from "rxjs";

@Component({
  selector: 'app-audio-progress-slider',
  standalone: true,
  imports: [
    DurationDisplayComponent,
    MatSliderModule,
    FormsModule,
    AsyncPipe,
    NgForOf
  ],
  templateUrl: './audio-progress-slider.component.html',
  styleUrl: './audio-progress-slider.component.scss'
})
export class AudioProgressSliderComponent implements OnInit, AfterViewInit, OnDestroy {
  private _unsubscribe$ = new Subject();
  private _currentTime: number = 0;

  constructor(public audioPlayer: AudioPlayerService) {
  }

  markers = [
    {position: 0},
    {position: 10},
    {position: 30},
    {position: 50},
    {position: 70},
    {position: 90},
    {position: 100},
  ]; // Section marker positions in percentage

  @ViewChild('slider')
  public slider?: ElementRef<MatSlider>;

  public ngOnInit(): void {
    // This is very important to avoid NG0100: Expression has changed after it was checked
    // The audio.currentTime can not be binded directly to the slider value as it changes outside of Angulars change detection
    this.audioPlayer.sampleCurrentTime$(500, false)
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(currentTime => {
        this._currentTime = currentTime;
      });
  }

  public ngAfterViewInit(): void {
    console.log(this.slider?.nativeElement);
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

  public sliderDragEnded(event: MatSliderDragEvent) {
    this.audioPlayer.seek(event.value);
  }

  public getWidth(marker: { position: number }, index: number): string {
    const nextMarker = index < this.markers.length - 1 ? this.markers[index + 1] : {position: 100};

    // console.log(this.sectionMarker?.nativeElement?.clientWidth);

    const width = nextMarker.position - marker.position;

    return `${width}%`;
  }
}
