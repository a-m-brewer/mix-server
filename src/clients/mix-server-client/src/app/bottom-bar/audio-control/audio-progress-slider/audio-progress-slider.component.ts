import {Component, OnDestroy, OnInit} from '@angular/core';
import {DurationDisplayComponent} from "../duration-display/duration-display.component";
import {MatSliderDragEvent, MatSliderModule} from "@angular/material/slider";
import {AudioPlayerService} from "../../../services/audio-player/audio-player.service";
import {FormsModule} from "@angular/forms";
import {AsyncPipe} from "@angular/common";
import {FlexModule} from "@angular/flex-layout";
import {Subject, takeUntil} from "rxjs";

@Component({
  selector: 'app-audio-progress-slider',
  standalone: true,
  imports: [
    DurationDisplayComponent,
    MatSliderModule,
    FormsModule,
    AsyncPipe,
    FlexModule
  ],
  templateUrl: './audio-progress-slider.component.html',
  styleUrl: './audio-progress-slider.component.scss'
})
export class AudioProgressSliderComponent implements OnInit, OnDestroy {
  private _unsubscribe$ = new Subject();
  private _currentTime: number = 0;

  constructor(public audioPlayer: AudioPlayerService) {
  }

  public ngOnInit(): void {
    // This is very important to avoid NG0100: Expression has changed after it was checked
    // The audio.currentTime can not be binded directly to the slider value as it changes outside of Angulars change detection
    this.audioPlayer.currentTime$
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(currentTime => {
        this._currentTime = currentTime;
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

  public sliderDragEnded(event: MatSliderDragEvent) {
    this.audioPlayer.seek(event.value);
  }
}
