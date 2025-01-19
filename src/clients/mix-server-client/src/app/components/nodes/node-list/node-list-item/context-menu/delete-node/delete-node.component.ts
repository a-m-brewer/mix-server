import {Component, forwardRef, Input} from '@angular/core';
import {MatIcon} from "@angular/material/icon";
import {MatMenuItem} from "@angular/material/menu";
import {NgIf} from "@angular/common";
import {ContextMenuButton} from "../context-menu-button";
import {FileExplorerFileNode} from "../../../../../../main-content/file-explorer/models/file-explorer-file-node";
import {MatDialog} from "@angular/material/dialog";
import {firstValueFrom} from "rxjs";
import {DeleteDialogComponent} from "../../../../../dialogs/delete-dialog/delete-dialog.component";
import {DeleteNodeService} from "../../../../../../services/nodes/delete-node.service";

@Component({
  selector: 'app-delete-node',
  standalone: true,
  imports: [
    MatIcon,
    MatMenuItem,
    NgIf
  ],
  templateUrl: './delete-node.component.html',
  styleUrl: './delete-node.component.scss',
  providers: [{provide: ContextMenuButton, useExisting: forwardRef(() => DeleteNodeComponent)}]
})
export class DeleteNodeComponent extends ContextMenuButton{
  public disabled: boolean = false;

  constructor(private _dialog: MatDialog,
              private _deleteNode: DeleteNodeService) {
    super();
  }

  @Input()
  public file?: FileExplorerFileNode;

  async deleteNode() {
    if (!this.file) {
      return;
    }

    const shouldDelete = await firstValueFrom(this._dialog.open(DeleteDialogComponent, {
      data: {
        displayName: this.file.name
      }
    }).afterClosed());

    if (shouldDelete) {
      await this._deleteNode.delete(this.file);
    }
  }
}
