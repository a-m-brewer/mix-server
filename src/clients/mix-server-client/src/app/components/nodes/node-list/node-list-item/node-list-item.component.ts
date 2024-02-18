import {Component, ContentChildren, EventEmitter, Input, Output, QueryList} from '@angular/core';
import {FileExplorerNodeType} from "../../../../main-content/file-explorer/enums/file-explorer-node-type";
import {ContextMenuButton} from "./context-menu/context-menu-button";
import {LoadingNodeStatus} from "../../../../services/repositories/models/loading-node-status";
import {NodeListItemChangedEvent} from "./enums/node-list-item-changed-event";
import {AudioPlayerStateModel} from "../../../../services/audio-player/models/audio-player-state-model";
import {FileExplorerPlayingState} from "../../../../main-content/file-explorer/enums/file-explorer-playing-state";
@Component({
  selector: 'app-node-list-item',
  templateUrl: './node-list-item.component.html',
  styleUrls: ['./node-list-item.component.scss']
})
export class NodeListItemComponent {
  protected readonly FileExplorerNodeType = FileExplorerNodeType;

  @ContentChildren(ContextMenuButton) contextMenuButtons: QueryList<ContextMenuButton> | null | undefined;

  @Input()
  public id: string = null!;

  @Input()
  public name: string = null!;

  @Input()
  public nodeType: FileExplorerNodeType = FileExplorerNodeType.Folder;

  @Input()
  public defaultIcon: string = 'folder';

  @Input()
  public loadingStatus: LoadingNodeStatus = {loading: false, loadingIds: []};

  @Input()
  public audioPlayerState: AudioPlayerStateModel = new AudioPlayerStateModel();

  @Input()
  public last: boolean = false;

  @Output()
  public contentClick = new EventEmitter<NodeListItemChangedEvent>();

  public get allContextMenuButtonsDisabled(): boolean {
    return !this.contextMenuButtons ||
      this.contextMenuButtons.length === 0 ||
      !this.contextMenuButtons.some(s => !s.disabled);
  }

  public get playingState(): FileExplorerPlayingState {
    if (this.audioPlayerState.queueItemId === this.id || this.audioPlayerState.node?.absolutePath === this.id) {
      return this.audioPlayerState.playing ? FileExplorerPlayingState.Playing : FileExplorerPlayingState.Paused;
    }

    return FileExplorerPlayingState.None;
  }

  public get isPlayingOrPaused(): boolean {
    return this.playingState !== FileExplorerPlayingState.None;
  }

  public onContentClicked(): void {
    if (this.loadingStatus.loading || this.isPlayingOrPaused) {
      console.log('returning');
      return;
    }

    console.log('emitting');
    this.contentClick.emit({id: this.id, nodeType: this.nodeType});
  }
}
