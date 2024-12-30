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
import {MatAnchor} from "@angular/material/button";
import {ControlDirtyMarkerComponent} from "../../components/forms/control-dirty-marker/control-dirty-marker.component";
import {AudioPlayerService} from "../../services/audio-player/audio-player.service";
import {combineLatestWith, Subject, takeUntil} from "rxjs";
import {timespanToTotalSeconds} from "../../utils/timespan-helpers";
import {
  CurrentPlaybackSessionRepositoryService
} from "../../services/repositories/current-playback-session-repository.service";
import {TracklistForm} from "../../services/tracklist/models/tracklist-form.interface";
import {PlaybackSession} from "../../services/repositories/models/playback-session";

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
  public session?: PlaybackSession | null;

  constructor(private _audioPlayerService: AudioPlayerService,
              private _sessionRepository: CurrentPlaybackSessionRepositoryService) {
  }

  public ngOnInit(): void {
    this._sessionRepository.currentSessionTracklistUpdated$
      .pipe()
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(session => {
        this.session = session;
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
}
