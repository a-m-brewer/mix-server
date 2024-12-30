import { Injectable } from '@angular/core';
import {
  ImportCueDto,
  ImportPlayerDto,
  ImportTrackDto,
  ImportTracklistDto
} from "../../generated-clients/mix-server-clients";
import {AbstractControl, FormArray, FormBuilder, FormControl, FormGroup} from "@angular/forms";
import {PlayersForm, TrackForm, TracklistCueForm, TracklistForm} from "../tracklist/models/tracklist-form.interface";

@Injectable({
  providedIn: 'root'
})
export class TracklistConverterService {

  constructor(private _formBuilder: FormBuilder) { }

  public createTracklistForm(dto?: ImportTracklistDto): FormGroup<TracklistForm> {
    const cues = dto?.cues ?? [];
    return this._formBuilder.group<TracklistForm>({
      cues: this._formBuilder.array<FormGroup<TracklistCueForm>>(cues.map(cue => this.createCueForm(cue)))
    });
  }

  public convertFormToDto(cues: FormArray<FormGroup<TracklistCueForm>>): ImportTracklistDto {
    return new ImportTracklistDto({
      cues: cues.controls.map(cue => this.convertCueFormToDto(cue))
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
