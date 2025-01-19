import {Component, forwardRef, Input} from '@angular/core';
import {MatIcon} from "@angular/material/icon";
import {MatMenuItem} from "@angular/material/menu";
import {NgIf} from "@angular/common";
import {ContextMenuButton} from "../context-menu-button";
import {FileExplorerFileNode} from "../../../../../../main-content/file-explorer/models/file-explorer-file-node";
import {CopyNodeService} from "../../../../../../services/nodes/copy-node.service";

@Component({
  selector: 'app-cut-node',
  standalone: true,
    imports: [
        MatIcon,
        MatMenuItem,
        NgIf
    ],
  templateUrl: './cut-node.component.html',
  styleUrl: './cut-node.component.scss',
  providers: [{provide: ContextMenuButton, useExisting: forwardRef(() => CutNodeComponent)}]
})
export class CutNodeComponent extends ContextMenuButton{
  public disabled: boolean = false;

  constructor(private _copyService: CopyNodeService) {
    super();
  }

  @Input()
  public file?: FileExplorerFileNode;

  cutNode() {
    if (!this.file) {
      return;
    }

    this._copyService.setSourceNode(this.file, true);
  }
}
