import {BehaviorSubject, Observable} from "rxjs";
import {FileExplorerNodeState} from "../enums/file-explorer-node-state.enum";
import {FileExplorerPlayingState} from "../enums/file-explorer-playing-state";

export interface FileExplorerNodeStateInterface {
  folderState: FileExplorerNodeState;

  isPlayingOrPaused: boolean;
  editing: boolean;

  selected: boolean;
  selected$: Observable<boolean>;
}

export class FileExplorerNodeStateClass implements FileExplorerNodeStateInterface{
  protected _selected$ = new BehaviorSubject<boolean>(false);
  protected _playing$ = new BehaviorSubject<FileExplorerPlayingState>(FileExplorerPlayingState.None);
  protected _editing$ = new BehaviorSubject<boolean>(false);
  protected _loading$ = new BehaviorSubject<boolean>(false);

  constructor(public absolutePath: string) {
  }

  public get folderState(): FileExplorerNodeState {
    if (this.loading) {
      return FileExplorerNodeState.Loading;
    }

    if (this.editing) {
      return FileExplorerNodeState.Editing;
    }

    if (this.playing === FileExplorerPlayingState.Playing) {
      return FileExplorerNodeState.Playing;
    }

    if (this.playing === FileExplorerPlayingState.Paused) {
      return FileExplorerNodeState.Paused;
    }

    return FileExplorerNodeState.None;
  }

  public get playing(): FileExplorerPlayingState {
    return this._playing$.value;
  }

  public set playing(value: FileExplorerPlayingState) {
    this._playing$.next(value);
  }

  public get isPlayingOrPaused(): boolean {
    return this._playing$.value !== FileExplorerPlayingState.None;
  }

  public get loading(): boolean {
    return this._loading$.value;
  }

  public set loading(value: boolean) {
    this._loading$.next(value);
  }

  public get editing(): boolean {
    return this._editing$.value;
  }

  public set editing(value: boolean) {
    this._editing$.next(value);
  }

  public get selected(): boolean {
    return this._selected$.value;
  }

  public get selected$(): Observable<boolean> {
    return this._selected$.asObservable();
  }

  public set selected(value: boolean) {
    this._selected$.next(value);
  }
}
