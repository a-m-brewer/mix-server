import {Component, OnDestroy, OnInit} from '@angular/core';
import {FormGroup, ReactiveFormsModule} from "@angular/forms";
import {MatDivider} from "@angular/material/divider";
import {NgClass, NgIf} from "@angular/common";
import {MatAnchor, MatIconButton} from "@angular/material/button";
import {ControlDirtyMarkerComponent} from "../../components/forms/control-dirty-marker/control-dirty-marker.component";
import {AudioPlayerService} from "../../services/audio-player/audio-player.service";
import {Subject, takeUntil} from "rxjs";
import {timespanToTotalSeconds} from "../../utils/timespan-helpers";
import {
  CurrentPlaybackSessionRepositoryService
} from "../../services/repositories/current-playback-session-repository.service";
import {TracklistCueForm, TracklistForm} from "../../services/tracklist/models/tracklist-form.interface";
import {MatIcon} from "@angular/material/icon";
import {MatTooltip} from "@angular/material/tooltip";

@Component({
  selector: 'app-tracklist-form',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatDivider,
    NgIf,
    MatAnchor,
    ControlDirtyMarkerComponent,
    NgClass,
    MatIcon,
    MatIconButton,
    MatTooltip
  ],
  templateUrl: './tracklist-form.component.html',
  styleUrl: './tracklist-form.component.scss'
})
export class TracklistFormComponent implements OnInit, OnDestroy {
  private _unsubscribe$ = new Subject<void>();

  public playingCueIndex: number = -1;
  public tracklistForm?: FormGroup<TracklistForm>;

  constructor(private _audioPlayerService: AudioPlayerService,
              private _sessionRepository: CurrentPlaybackSessionRepositoryService) {
  }

  public ngOnInit(): void {
    this._sessionRepository.currentSessionTracklistChanged$
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(session => {
        if (!session) {
          console.error('TracklistFormComponent: No session found');
          this.tracklistForm = undefined;
          return;
        }

        if (!session.currentNode.metadata?.mediaInfo) {
          console.error('TracklistFormComponent: No mediaInfo found in current node');
          this.tracklistForm = undefined;
          return;
        }

        this.tracklistForm = session.currentNode.metadata.mediaInfo.tracklist
      });

    this._audioPlayerService.currentCueIndex$
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(cueIndex => {
        this.playingCueIndex = cueIndex;
      });
  }

  public ngOnDestroy(): void {
    this._unsubscribe$.next();
    this._unsubscribe$.complete();
  }

  playCue(cue: FormGroup<TracklistCueForm>) {
    const cueValue = cue.value.cue;
    if (!cueValue) {
      return;
    }

    const cueTotalSeconds = timespanToTotalSeconds(cueValue);

    this._audioPlayerService.seek(cueTotalSeconds);
  }
}
