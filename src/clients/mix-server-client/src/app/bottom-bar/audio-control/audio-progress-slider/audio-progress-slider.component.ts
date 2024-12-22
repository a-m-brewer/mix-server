import {Component, OnDestroy, OnInit} from '@angular/core';
import {DurationDisplayComponent} from "../duration-display/duration-display.component";
import {MatSliderModule} from "@angular/material/slider";
import {AudioPlayerService} from "../../../services/audio-player/audio-player.service";
import {FormsModule} from "@angular/forms";
import {Subject, takeUntil} from "rxjs";
import {AudioSliderComponent} from "../audio-slider/audio-slider.component";

@Component({
  selector: 'app-audio-progress-slider',
  standalone: true,
  imports: [
    DurationDisplayComponent,
    MatSliderModule,
    FormsModule,
    AudioSliderComponent,
  ],
  templateUrl: './audio-progress-slider.component.html',
  styleUrl: './audio-progress-slider.component.scss'
})
export class AudioProgressSliderComponent implements OnInit, OnDestroy {
  private _unsubscribe$ = new Subject();

  constructor(public audioPlayer: AudioPlayerService) {
  }

  public currentTime: number = 0;
  public duration: number = 0;

  public ngOnInit(): void {
    this.audioPlayer.sampleCurrentTime$(500, false)
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(currentTime => {
        this.currentTime = currentTime;
      });

    this.audioPlayer.duration$
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(duration => {
        this.duration = duration;
      });
  }

  public ngOnDestroy(): void {
    this._unsubscribe$.next(null);
    this._unsubscribe$.complete();
  }
}
