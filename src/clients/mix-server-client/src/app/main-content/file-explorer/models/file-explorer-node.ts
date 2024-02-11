import {FileExplorerNodeType} from "../enums/file-explorer-node-type";
import {FileExplorerNodeState} from "../enums/file-explorer-node-state.enum";
import {NodeListItem} from "../../../components/nodes/node-list/node-list-item/models/node-list-item";
import {BehaviorSubject, Observable} from "rxjs";

export abstract class FileExplorerNode implements NodeListItem {
  protected _selected$ = new BehaviorSubject<boolean>(false);
  private _state: FileExplorerNodeState = FileExplorerNodeState.None;

  protected constructor(public name: string,
                        public nameIdentifier: string,
                        public absolutePath: string | null | undefined,
                        public type: FileExplorerNodeType,
                        public exists: boolean,
                        public mdIcon: string,
                        public imageUrl: string | undefined) {
  }

  public abstract get disabled(): boolean;

  public get selected(): boolean {
    return this._selected$.getValue();
  }

  public get selected$(): Observable<boolean> {
    return this._selected$.asObservable();
  }

  public set selected(value: boolean) {
    if (this.selected === value) {
      return;
    }

    this._selected$.next(value);
  }

  public get state(): FileExplorerNodeState {
    return this._state;
  }

  public set state(value: FileExplorerNodeState) {
    if (this._state === value) {
      return;
    }

    this._state = value;
  }

  public get playing(): boolean {
    return this.state === FileExplorerNodeState.Playing;
  }

  public get paused(): boolean {
    return this.state === FileExplorerNodeState.Paused;
  }

  public get editing(): boolean {
    return this.state === FileExplorerNodeState.Editing;
  }

  public get isCurrentSession(): boolean {
    return this.playing || this.paused;
  }

  public get defaultState(): boolean {
    return this.state === FileExplorerNodeState.None
  }

  public get loading(): boolean {
    return this.state === FileExplorerNodeState.Loading;
  }

  public abstract isEqual(other: FileExplorerNode): boolean;
}
