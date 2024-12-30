import {Component, Input, OnDestroy, OnInit} from '@angular/core';
import {DurationDisplayComponent} from "../duration-display/duration-display.component";
import {MatSliderModule} from "@angular/material/slider";
import {AudioPlayerService} from "../../../services/audio-player/audio-player.service";
import {FormsModule} from "@angular/forms";
import {combineLatestWith, Subject, takeUntil} from "rxjs";
import {AudioSliderComponent} from "../audio-slider/audio-slider.component";
import {WindowSizeRepositoryService} from "../../../services/repositories/window-size-repository.service";

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

  constructor(public audioPlayer: AudioPlayerService,
              private _windowSizeRepository: WindowSizeRepositoryService) {
  }

  public currentTime: number = 0;
  public duration: number = 0;

  @Input()
  public onMobile: boolean = false;

  public ngOnInit(): void {
    this.audioPlayer.sampleCurrentTime$(500, false)
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(currentTime => {
        this.currentTime = currentTime;
      });

    this.audioPlayer.duration$
      .pipe(combineLatestWith(this._windowSizeRepository.windowType$))
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(([duration, windowSize]) => {
        console.log('duration', duration, windowSize);
        this.duration = duration;
      });
  }

  public ngOnDestroy(): void {
    this._unsubscribe$.next(null);
    this._unsubscribe$.complete();
  }
}
