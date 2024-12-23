import { Injectable } from '@angular/core';
import {
  ImportCueDto, ImportPlayerDto, ImportTrackDto, ImportTracklistDto,
  ImportTracklistResponse, SaveTracklistCommand,
  TracklistClient,
  TracklistPlayerType
} from "../../generated-clients/mix-server-clients";
import {LoadingRepositoryService} from "../repositories/loading-repository.service";
import {ToastService} from "../toasts/toast-service";
import {firstValueFrom} from "rxjs";
import {AbstractControl, FormArray, FormBuilder, FormControl, FormGroup} from "@angular/forms";
import {CurrentPlaybackSessionRepositoryService} from "../repositories/current-playback-session-repository.service";

export interface PlayersForm {
  type: FormControl<TracklistPlayerType>;
  urls: FormArray<FormControl<string>>;
}

export interface TrackForm {
  name: FormControl<string>;
  artist: FormControl<string>;
  players: FormArray<FormGroup<PlayersForm>>;
}

export interface TracklistCueForm {
  cue: FormControl<string>;
  tracks: FormArray<FormGroup<TrackForm>>;
}

export interface TracklistForm {
  cues: FormArray<FormGroup<TracklistCueForm>>;
}

@Injectable({
  providedIn: 'root'
})
export class TracklistFormService {
  public form: FormGroup<TracklistForm>;

  constructor(private _client: TracklistClient,
              private _formBuilder: FormBuilder,
              private _loading: LoadingRepositoryService,
              private _sessionRepository: CurrentPlaybackSessionRepositoryService,
              private _toastService: ToastService) {
    this.form = this.createTracklistForm();

    this._sessionRepository
      .currentSession$
      .subscribe(session => {
        if (session) {
          this.form = this.createTracklistForm(session.tracklist);
        }
      })
  }

  public get cues(): FormArray<FormGroup<TracklistCueForm>> {
    return this.form.controls.cues as FormArray<FormGroup<TracklistCueForm>>;
  }

  public async importTracklistFile(file: File): Promise<void> {
    this._loading.startLoading();

    try {
      const dto = await firstValueFrom(this._client.importTracklist({
        fileName: file.name,
        data: file as Blob
      }));
      this.form = this.createTracklistForm(dto.tracklist);
      this.markAllAsDirty(this.form);
    } catch (err) {
      this._toastService.logServerError(err, 'Failed to import tracklist file');
    } finally {
      this._loading.stopLoading();
    }
  }


  public async saveTracklist(): Promise<void> {
    this._loading.startLoading();

    try {
      const dto = await firstValueFrom(this._client.saveTracklist(new SaveTracklistCommand({
        tracklist: this.convertFormToDto()
      })));
      this.form = this.createTracklistForm(dto.tracklist);
    } catch (err) {
      this._toastService.logServerError(err, 'Failed to save tracklist');
    } finally {
      this._loading.stopLoading();
    }
  }

  private createTracklistForm(dto?: ImportTracklistDto): FormGroup<TracklistForm> {
    const cues = dto?.cues ?? [];
    return this._formBuilder.group<TracklistForm>({
      cues: this._formBuilder.array<FormGroup<TracklistCueForm>>(cues.map(cue => this.createCueForm(cue)))
    });
  }

  private createCueForm(cue: ImportCueDto): FormGroup<TracklistCueForm> {
    return this._formBuilder.group<TracklistCueForm>({
      cue: this._formBuilder.nonNullable.control(cue.cue),
      tracks: this._formBuilder.array<FormGroup<TrackForm>>(cue.tracks.map(track => this.createTrackForm(track)))
    });
  }

  private createTrackForm(track: ImportTrackDto): FormGroup<TrackForm> {
    return this._formBuilder.group<TrackForm>({
      name: this._formBuilder.nonNullable.control(track.name),
      artist: this._formBuilder.nonNullable.control(track.artist),
      players: this._formBuilder.array<FormGroup<PlayersForm>>(track.players.map(player => this.createPlayerForm(player)))
    });
  }

  private createPlayerForm(player: ImportPlayerDto): FormGroup<PlayersForm> {
    return this._formBuilder.group<PlayersForm>({
      type: this._formBuilder.nonNullable.control(player.type),
      urls: this._formBuilder.array<FormControl<string>>(player.urls.map(url => this._formBuilder.nonNullable.control(url)))
    });
  }

  private markAllAsDirty(control: AbstractControl) {
    control.markAsDirty(); // Mark the current control as dirty
    if (control instanceof FormGroup) {
      Object.keys(control.controls).forEach((key) => {
        this.markAllAsDirty(control.controls[key]); // Recursively mark child controls
      });
    } else if (control instanceof FormArray) {
      control.controls.forEach((childControl) => {
        this.markAllAsDirty(childControl); // Recursively mark child controls
      });
    }
  }

  private convertFormToDto(): ImportTracklistDto {
    return new ImportTracklistDto({
      cues: this.cues.controls.map(cue => this.convertCueFormToDto(cue))
    });
  }

  private convertCueFormToDto(cue: FormGroup<TracklistCueForm>): ImportCueDto {
    return new ImportCueDto({
      cue: cue.controls.cue.value,
      tracks: cue.controls.tracks.controls.map(track => this.convertTrackFormToDto(track))
    });
  }

  private convertTrackFormToDto(track: FormGroup<TrackForm>): ImportTrackDto {
    return new ImportTrackDto({
      name: track.controls.name.value,
      artist: track.controls.artist.value,
      players: track.controls.players.controls.map(player => this.convertPlayerFormToDto(player))
    });
  }

  private convertPlayerFormToDto(player: FormGroup<PlayersForm>): ImportPlayerDto {
    return new ImportPlayerDto({
      type: player.controls.type.value,
      urls: player.controls.urls.controls.map(url => url.value)
    });
  }
}
