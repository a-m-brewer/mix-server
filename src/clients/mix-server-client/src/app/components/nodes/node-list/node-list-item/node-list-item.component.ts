import {Component, ContentChildren, EventEmitter, Input, Output, QueryList} from '@angular/core';
import {FileExplorerNodeType} from "../../../../main-content/file-explorer/enums/file-explorer-node-type";
import {ContextMenuButton} from "./context-menu/context-menu-button";
import {LoadingNodeStatus, LoadingNodeStatusImpl} from "../../../../services/repositories/models/loading-node-status";
import {NodeListItemChangedEvent} from "./interfaces/node-list-item-changed-event";
import {AudioPlayerStateModel} from "../../../../services/audio-player/models/audio-player-state-model";
import {FileExplorerPlayingState} from "../../../../main-content/file-explorer/enums/file-explorer-playing-state";
import {NodeListItemSelectedEvent} from "./interfaces/node-list-item-selected-event";
@Component({
  selector: 'app-node-list-item',
  templateUrl: './node-list-item.component.html',
  styleUrls: ['./node-list-item.component.scss']
})
export class NodeListItemComponent {
  protected readonly FileExplorerNodeType = FileExplorerNodeType;

  @ContentChildren(ContextMenuButton) contextMenuButtons: QueryList<ContextMenuButton> | null | undefined;

  @Input()
  public key: string = null!;

  @Input()
  public nodeType: FileExplorerNodeType = FileExplorerNodeType.Folder;

  @Input()
  public defaultIcon: string = 'folder';

  @Input()
  public loadingStatus: LoadingNodeStatus = LoadingNodeStatusImpl.new;

  @Input()
  public audioPlayerState: AudioPlayerStateModel = new AudioPlayerStateModel();

  @Input()
  public editing: boolean = false;

  @Input()
  public selectable: boolean = false;

  @Input()
  public disabled: boolean = false;

  @Input()
  public last: boolean = false;

  @Output()
  public contentClick = new EventEmitter<NodeListItemChangedEvent>();

  @Output()
  public selectedChange = new EventEmitter<NodeListItemSelectedEvent>();

  public get allContextMenuButtonsDisabled(): boolean {
    return !this.contextMenuButtons ||
      this.contextMenuButtons.length === 0 ||
      !this.contextMenuButtons.some(s => !s.disabled);
  }

  public get playingState(): FileExplorerPlayingState {
    if (this.audioPlayerState.queueItemId === this.key || this.audioPlayerState.node?.path.key === this.key) {
      return this.audioPlayerState.playing ? FileExplorerPlayingState.Playing : FileExplorerPlayingState.Paused;
    }

    return FileExplorerPlayingState.None;
  }

  public get isPlayingOrPaused(): boolean {
    return this.playingState !== FileExplorerPlayingState.None;
  }

  public onContentClicked(): void {
    if (this.loadingStatus.loading || this.isPlayingOrPaused || this.disabled || this.editing) {
      return;
    }

    this.contentClick.emit({key: this.key, nodeType: this.nodeType});
  }

  public onSelectedChange(selected: boolean): void {
    this.selectedChange.emit({key: this.key, selected});
  }
}
