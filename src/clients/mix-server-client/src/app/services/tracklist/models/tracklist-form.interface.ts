import {FormArray, FormControl, FormGroup} from "@angular/forms";
import {TracklistPlayerType} from "../../../generated-clients/mix-server-clients";

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
