import {Component, OnDestroy, OnInit} from '@angular/core';
import {MatButton} from "@angular/material/button";
import {TracklistFormService} from "../../services/tracklist/tracklist-form.service";
import {ImportTracklistFormComponent} from "./import-tracklist-form/import-tracklist-form.component";
import {SaveTracklistFormComponent} from "./save-tracklist-form/save-tracklist-form.component";
import {NgIf} from "@angular/common";
import {
  CurrentPlaybackSessionRepositoryService
} from "../../services/repositories/current-playback-session-repository.service";
import {Subject, takeUntil} from "rxjs";
import { FormGroup } from '@angular/forms';
import {TracklistForm} from "../../services/tracklist/models/tracklist-form.interface";

@Component({
  selector: 'app-tracklist-toolbar',
  standalone: true,
  imports: [
    ImportTracklistFormComponent,
    SaveTracklistFormComponent,
    NgIf
  ],
  templateUrl: './tracklist-toolbar.component.html',
  styleUrl: './tracklist-toolbar.component.scss'
})
export class TracklistToolbarComponent implements OnInit, OnDestroy {
  private _unsubscribe: Subject<void> = new Subject<void>();

  public form: FormGroup<TracklistForm> | undefined;

  constructor(private _sessionRepository: CurrentPlaybackSessionRepositoryService) {
  }

  public ngOnInit(): void {
    this._sessionRepository.currentSession$
      .pipe(takeUntil(this._unsubscribe))
      .subscribe((session) => {
        this.form = session?.tracklist;
      });
  }

  public ngOnDestroy(): void {
    this._unsubscribe.next();
    this._unsubscribe.complete();
  }
}
