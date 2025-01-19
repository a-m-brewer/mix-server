import {Component, forwardRef, Input} from '@angular/core';
import {MatIcon} from "@angular/material/icon";
import {MatMenuItem} from "@angular/material/menu";
import {ContextMenuButton} from "../context-menu-button";
import {FileExplorerFileNode} from "../../../../../../main-content/file-explorer/models/file-explorer-file-node";
import {NgIf} from "@angular/common";
import {CopyNodeService} from "../../../../../../services/nodes/copy-node.service";

@Component({
  selector: 'app-copy-node',
  standalone: true,
  imports: [
    MatIcon,
    MatMenuItem,
    NgIf
  ],
  templateUrl: './copy-node.component.html',
  styleUrl: './copy-node.component.scss',
  providers: [{provide: ContextMenuButton, useExisting: forwardRef(() => CopyNodeComponent)}]
})
export class CopyNodeComponent extends ContextMenuButton {
  public disabled: boolean = false;

  constructor(private _copyService: CopyNodeService) {
    super();
  }

  @Input()
  public file?: FileExplorerFileNode

  copyNode() {
    if (!this.file) {
      return;
    }

    this._copyService.setSourceNode(this.file, false);
  }
}
