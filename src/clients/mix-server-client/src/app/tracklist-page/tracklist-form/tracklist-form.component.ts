import {Component, OnDestroy, OnInit} from '@angular/core';
import {TracklistFormService} from "../../services/tracklist/tracklist-form.service";
import {FormArray, ReactiveFormsModule} from "@angular/forms";
import {
  MatList,
  MatListItem,
  MatListItemLine, MatListItemMeta,
  MatListItemTitle,
  MatListSubheaderCssMatStyler
} from "@angular/material/list";
import {MatDivider} from "@angular/material/divider";
import {NgClass, NgIf} from "@angular/common";
import {MatAnchor} from "@angular/material/button";
import {ControlDirtyMarkerComponent} from "../../components/forms/control-dirty-marker/control-dirty-marker.component";
import {AudioPlayerService} from "../../services/audio-player/audio-player.service";
import {Subject, takeUntil} from "rxjs";
import {timespanToTotalSeconds} from "../../utils/timespan-helpers";

@Component({
  selector: 'app-tracklist-form',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatList,
    MatListItem,
    MatListItemTitle,
    MatListItemLine,
    MatDivider,
    NgIf,
    MatListSubheaderCssMatStyler,
    MatListItemMeta,
    MatAnchor,
    ControlDirtyMarkerComponent,
    NgClass
  ],
  templateUrl: './tracklist-form.component.html',
  styleUrl: './tracklist-form.component.scss'
})
export class TracklistFormComponent implements OnInit, OnDestroy {
  private _unsubscribe$ = new Subject<void>();

  public playingCueIndex: number = -1;

  constructor(public formService: TracklistFormService,
              private _audioPlayerService: AudioPlayerService) {
  }

  public ngOnInit(): void {
    this._audioPlayerService.sampleCurrentTime$(500, false)
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(currentTime => {
        for (let i = this.formService.cues.controls.length - 1; i >= 0; i--) {
          const cue = this.formService.cues.controls[i].value.cue;
          if (!cue) {
            continue;
          }

          const cueStartTimeSeconds = timespanToTotalSeconds(cue);

          if (cueStartTimeSeconds <= currentTime) {
            this.playingCueIndex = i;
            return;
          }
        }

        this.playingCueIndex = -1;
      });
  }

  public ngOnDestroy(): void {
    this._unsubscribe$.next();
    this._unsubscribe$.complete();
  }
}
