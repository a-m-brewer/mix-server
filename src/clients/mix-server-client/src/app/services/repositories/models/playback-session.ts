import {FileExplorerFileNode} from "../../../main-content/file-explorer/models/file-explorer-file-node";
import {PlaybackState} from "./playback-state";
import {ImportTracklistDto} from "../../../generated-clients/mix-server-clients";
import {TracklistCueForm, TracklistForm} from "../../tracklist/models/tracklist-form.interface";
import {FormArray, FormGroup} from "@angular/forms";

export interface IPlaybackSession {
  id: string;
  currentNode: FileExplorerFileNode,
  lastPlayed: Date,
  deviceId: string | null | undefined,
  autoPlay: boolean
  tracklist: FormGroup<TracklistForm>;
}

export class PlaybackSession implements IPlaybackSession {
  constructor(public id: string,
              public currentNode: FileExplorerFileNode,
              public lastPlayed: Date,
              public state: PlaybackState,
              public autoPlay: boolean,
              public tracklist: FormGroup<TracklistForm>) {
  }

  public static copy(session: IPlaybackSession,
                     state: PlaybackState) : PlaybackSession {
    return new PlaybackSession(
      session.id,
      session.currentNode,
      session.lastPlayed,
      state,
      session.autoPlay,
      session.tracklist);
  }

  public get deviceId(): string | null | undefined {
    return this.state.deviceId;
  }

  public set deviceId(value: string | null | undefined) {
    this.state.deviceId = value;
  }

  public get cues(): FormArray<FormGroup<TracklistCueForm>> {
    return this.tracklist.controls.cues as FormArray<FormGroup<TracklistCueForm>>;
  }
}
