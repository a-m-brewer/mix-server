import {FileExplorerFileNode} from "../../../main-content/file-explorer/models/file-explorer-file-node";
import {PlaybackState} from "./playback-state";
import {ImportTracklistDto} from "../../../generated-clients/mix-server-clients";
import {TracklistCueForm, TracklistForm} from "../../tracklist/models/tracklist-form.interface";
import {FormArray, FormGroup} from "@angular/forms";
import {Observable, Subject, takeUntil} from "rxjs";

export interface IPlaybackSession {
  id: string;
  currentNode: FileExplorerFileNode,
  currentNode$: Observable<FileExplorerFileNode>
  lastPlayed: Date,
  deviceId: string | null | undefined,
  autoPlay: boolean
}

export class PlaybackSession implements IPlaybackSession {
  private _unsubscribe$ = new Subject<void>();

  constructor(public id: string,
              initialNode: FileExplorerFileNode,
              public currentNode$: Observable<FileExplorerFileNode>,
              public lastPlayed: Date,
              public state: PlaybackState,
              public autoPlay: boolean) {
    this.currentNode = initialNode;
    currentNode$.pipe(takeUntil(this._unsubscribe$)).subscribe(node => {
      this.currentNode = node;
    });
  }

  public currentNode: FileExplorerFileNode;

  public destroy(): void {
    this._unsubscribe$.next();
    this._unsubscribe$.complete();
  }

  public static copy(session: IPlaybackSession,
                     state: PlaybackState) : PlaybackSession {
    return new PlaybackSession(
      session.id,
      session.currentNode,
      session.currentNode$,
      session.lastPlayed,
      state,
      session.autoPlay);
  }

  public get deviceId(): string | null | undefined {
    return this.state.deviceId;
  }

  public set deviceId(value: string | null | undefined) {
    this.state.deviceId = value;
  }
}
