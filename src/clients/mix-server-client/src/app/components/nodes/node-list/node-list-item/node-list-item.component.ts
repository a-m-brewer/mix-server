import {Component, ContentChildren, EventEmitter, Input, Output, QueryList} from '@angular/core';
import {FileExplorerNodeType} from "../../../../main-content/file-explorer/enums/file-explorer-node-type";
import {ContextMenuButton} from "./context-menu/context-menu-button";
import {FileExplorerNode} from "../../../../main-content/file-explorer/models/file-explorer-node";

@Component({
  selector: 'app-node-list-item',
  templateUrl: './node-list-item.component.html',
  styleUrls: ['./node-list-item.component.scss']
})
export class NodeListItemComponent {
  protected readonly FileExplorerNodeType = FileExplorerNodeType;

  @ContentChildren(ContextMenuButton) contextMenuButtons: QueryList<ContextMenuButton> | null | undefined;

  @Input()
  public node: FileExplorerNode = undefined!;

  @Input()
  public loading: boolean = false;

  @Input()
  public editing: boolean = false;

  @Input()
  public last: boolean = false;

  @Output()
  public click = new EventEmitter<FileExplorerNode>();

  public get allContextMenuButtonsDisabled(): boolean {
    return !this.contextMenuButtons ||
      this.contextMenuButtons.length === 0 ||
      !this.contextMenuButtons.some(s => !s.disabled);
  }

  public onContentClicked(): void {
    if (this.node.disabled || this.node.state.editing || this.loading) {
      return;
    }

    this.click.emit(this.node);
  }
}
