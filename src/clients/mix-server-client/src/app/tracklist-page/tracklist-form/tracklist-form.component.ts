import {Component, OnDestroy, OnInit} from '@angular/core';
import {TracklistFormService} from "../../services/tracklist/tracklist-form.service";
import {FormArray, FormGroup, ReactiveFormsModule} from "@angular/forms";
import {
  MatList,
  MatListItem,
  MatListItemLine, MatListItemMeta,
  MatListItemTitle,
  MatListSubheaderCssMatStyler
} from "@angular/material/list";
import {MatDivider} from "@angular/material/divider";
import {NgClass, NgIf} from "@angular/common";
import {MatAnchor, MatIconButton} from "@angular/material/button";
import {ControlDirtyMarkerComponent} from "../../components/forms/control-dirty-marker/control-dirty-marker.component";
import {AudioPlayerService} from "../../services/audio-player/audio-player.service";
import {combineLatestWith, Subject, takeUntil} from "rxjs";
import {timespanToTotalSeconds} from "../../utils/timespan-helpers";
import {
  CurrentPlaybackSessionRepositoryService
} from "../../services/repositories/current-playback-session-repository.service";
import {TracklistCueForm, TracklistForm} from "../../services/tracklist/models/tracklist-form.interface";
import {PlaybackSession} from "../../services/repositories/models/playback-session";
import {MatIcon} from "@angular/material/icon";
import {MatTooltip} from "@angular/material/tooltip";
import {MediaMetadata} from "../../main-content/file-explorer/models/media-metadata";

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
        if (session?.currentNode && session.currentNode.metadata instanceof MediaMetadata) {
          this.tracklistForm = session.currentNode.metadata.tracklist;
        } else {
          this.tracklistForm = undefined;
        }
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
