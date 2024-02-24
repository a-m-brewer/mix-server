import { Component } from '@angular/core';
import {DurationDisplayComponent} from "../duration-display/duration-display.component";
import {MatSliderDragEvent, MatSliderModule} from "@angular/material/slider";
import {AudioPlayerService} from "../../../services/audio-player/audio-player.service";
import {FormsModule} from "@angular/forms";
import {AsyncPipe} from "@angular/common";
import {FlexModule} from "@angular/flex-layout";

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
export class AudioProgressSliderComponent {
  constructor(public audioPlayer: AudioPlayerService) {
  }

  public sliderDragEnded(event: MatSliderDragEvent) {
    this.audioPlayer.seek(event.value);
  }
}
