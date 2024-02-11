import {Component, EventEmitter, Input, OnDestroy, OnInit, Output} from '@angular/core';
import {FileExplorerNodeState} from "../../../../../main-content/file-explorer/enums/file-explorer-node-state.enum";
import {FormControl} from "@angular/forms";

@Component({
  selector: 'app-node-list-item-icon',
  templateUrl: './node-list-item-icon.component.html',
  styleUrls: ['./node-list-item-icon.component.scss']
})
export class NodeListItemIcon {

  @Input()
  public state: FileExplorerNodeState = FileExplorerNodeState.None;

  @Input()
  public defaultIcon: string = 'folder';

  @Input()
  public selected: boolean = false;

  @Output()
  public selectedChange = new EventEmitter<boolean>();

  public get playing(): boolean {
    return this.state === FileExplorerNodeState.Playing;
  }

  public get paused(): boolean {
    return this.state === FileExplorerNodeState.Paused;
  }

  public get currentlyPlaying(): boolean {
    return this.playing || this.paused;
  }

  public get defaultState(): boolean {
    return this.state === FileExplorerNodeState.None
  }

  public get loading(): boolean {
    return this.state === FileExplorerNodeState.Loading;
  }

  public get editing(): boolean {
    return this.state === FileExplorerNodeState.Editing;
  }

  public onEditCheckboxClicked() {
    this.selected = !this.selected;
    this.selectedChange.emit(this.selected);
  }
}
