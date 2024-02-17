import {BehaviorSubject, Observable} from "rxjs";
import {FileExplorerNodeState} from "../enums/file-explorer-node-state.enum";

export interface FileExplorerNodeStateInterface {
  folderState: FileExplorerNodeState;
  folderState$: Observable<FileExplorerNodeState>;

  isPlayingOrPaused: boolean;
  editing: boolean;

  selected: boolean;
  selected$: Observable<boolean>;
}

export class FileExplorerNodeStateClass implements FileExplorerNodeStateInterface{
  protected _selected$ = new BehaviorSubject<boolean>(false);
  private _fileExplorerNodeState$ = new BehaviorSubject<FileExplorerNodeState>(FileExplorerNodeState.None);

  constructor(public absolutePath: string) {
  }

  public get folderState(): FileExplorerNodeState {
    return this._fileExplorerNodeState$.value;
  }

  public set folderState(value: FileExplorerNodeState) {
    this._fileExplorerNodeState$.next(value);
  }

  public get folderState$() {
    return this._fileExplorerNodeState$.asObservable();
  }

  public get isPlayingOrPaused(): boolean {
    return this.folderState === FileExplorerNodeState.Playing || this.folderState === FileExplorerNodeState.Paused;
  }

  public get editing(): boolean {
    return this.folderState === FileExplorerNodeState.Editing;
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
