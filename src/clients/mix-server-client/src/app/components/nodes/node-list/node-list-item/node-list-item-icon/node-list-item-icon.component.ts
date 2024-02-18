import {Component, EventEmitter, Input, Output} from '@angular/core';
import {FileExplorerPlayingState} from "../../../../../main-content/file-explorer/enums/file-explorer-playing-state";

@Component({
  selector: 'app-node-list-item-icon',
  templateUrl: './node-list-item-icon.component.html',
  styleUrls: ['./node-list-item-icon.component.scss']
})
export class NodeListItemIcon {
  protected readonly FileExplorerPlayingState = FileExplorerPlayingState;

  @Input()
  public defaultIcon: string = 'folder';

  @Input()
  public loading: boolean = false;

  @Input()
  public disabled: boolean = false;

  @Input()
  public playingState: FileExplorerPlayingState = FileExplorerPlayingState.None;

  @Input()
  public selected: boolean = false;

  @Output()
  public selectedChange = new EventEmitter<boolean>();

  public onEditCheckboxClicked() {
    this.selected = !this.selected;
    this.selectedChange.emit(this.selected);
  }
}
