import {FileExplorerFileNode} from "../../../main-content/file-explorer/models/file-explorer-file-node";
import {PlaybackState} from "./playback-state";
import {ImportTracklistDto, StreamKeyDto} from "../../../generated-clients/mix-server-clients";
import {TracklistCueForm, TracklistForm} from "../../tracklist/models/tracklist-form.interface";
import {FormArray, FormGroup} from "@angular/forms";
import {Observable, Subject, takeUntil} from "rxjs";

export interface IPlaybackSession {
  id: string;
  currentNode: FileExplorerFileNode,
  currentNode$: Observable<FileExplorerFileNode>
  deviceId: string | null | undefined,
  streamKey: StreamKeyDto,
}

export class PlaybackSession implements IPlaybackSession {
  private _unsubscribe$ = new Subject<void>();

  constructor(public id: string,
              initialNode: FileExplorerFileNode,
              public currentNode$: Observable<FileExplorerFileNode>,
              public state: PlaybackState,
              public streamKey: StreamKeyDto) {
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
      state,
      new StreamKeyDto({
        key: session.streamKey.key,
        expires: session.streamKey.expires
      }));
  }

  public get deviceId(): string | null | undefined {
    return this.state.deviceId;
  }

  public set deviceId(value: string | null | undefined) {
    this.state.deviceId = value;
  }
}
