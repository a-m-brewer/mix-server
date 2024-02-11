import {FileExplorerFileNode} from "../../../main-content/file-explorer/models/file-explorer-file-node";
import {NodeListItem} from "../../../components/nodes/node-list/node-list-item/models/node-list-item";
import {FileExplorerNodeState} from "../../../main-content/file-explorer/enums/file-explorer-node-state.enum";
import {FileExplorerNodeType, QueueSnapshotItemType} from "../../../generated-clients/mix-server-clients";
import {AudioPlayerState} from "../../audio-player/models/audio-player-state";
import {Observable} from "rxjs";

export class QueueItem implements NodeListItem {
  constructor(public id: string,
              public itemType: QueueSnapshotItemType,
              public file: FileExplorerFileNode,
              public isCurrentQueuePosition: boolean) {
  }

  public get disabled(): boolean {
    return this.isCurrentQueuePosition;
  }

  public get selected(): boolean {
    return this.file.selected;
  }

  public set selected(value: boolean) {
    this.file.selected = value;
  }

  public get selected$(): Observable<boolean> {
    return this.file.selected$;
  }

  public get editing(): boolean {
    return this.file.state === FileExplorerNodeState.Editing;
  }

  public get isCurrentSession(): boolean {
    return this.isCurrentQueuePosition;
  }

  public get mdIcon(): string {
    return this.file.mdIcon;
  }

  public get name(): string {
    return this.file.name;
  }

  public get state(): FileExplorerNodeState {
    return this.file.state;
  }

  public set state(value: FileExplorerNodeState) {
    this.file.state = value;
  }

  public get type(): FileExplorerNodeType {
    return this.file.type;
  }

  public updateState(state: AudioPlayerState | null | undefined, globalEditMode: boolean) {
    if (this.isCurrentQueuePosition && state) {
      this.state = state.playing ? FileExplorerNodeState.Playing : FileExplorerNodeState.Paused;
      return;
    }

    if (globalEditMode && this.itemType == QueueSnapshotItemType.User) {
      this.state = FileExplorerNodeState.Editing;
      return;
    }

    this.state = FileExplorerNodeState.None;
  }

  public restoreState(previousItem: QueueItem) {
    this.selected = previousItem.selected;
  }
}
