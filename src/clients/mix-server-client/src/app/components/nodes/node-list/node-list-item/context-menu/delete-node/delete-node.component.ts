import {Component, forwardRef, Input, OnInit, OnDestroy} from '@angular/core';
import {MatIcon} from "@angular/material/icon";
import {MatMenuItem} from "@angular/material/menu";
import {NgIf} from "@angular/common";
import {ContextMenuButton} from "../context-menu-button";
import {FileExplorerFileNode} from "../../../../../../main-content/file-explorer/models/file-explorer-file-node";
import {MatDialog} from "@angular/material/dialog";
import {firstValueFrom, Subject, takeUntil} from "rxjs";
import {DeleteDialogComponent} from "../../../../../dialogs/delete-dialog/delete-dialog.component";
import {DeleteNodeService} from "../../../../../../services/nodes/delete-node.service";
import {RoleRepositoryService} from "../../../../../../services/repositories/role-repository.service";
import {Role} from "../../../../../../generated-clients/mix-server-clients";

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
export class DeleteNodeComponent extends ContextMenuButton implements OnInit, OnDestroy {
  private _unsubscribe$ = new Subject<void>();

  public disabled: boolean = false;

  constructor(private _dialog: MatDialog,
              private _deleteNode: DeleteNodeService,
              private _roleRepository: RoleRepositoryService) {
    super();
  }

  @Input()
  public file?: FileExplorerFileNode;

  ngOnInit() {
    this._roleRepository.inRole$(Role.Administrator)
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(isAdmin => this.disabled = !isAdmin);
  }

  ngOnDestroy() {
    this._unsubscribe$.next();
    this._unsubscribe$.complete();
  }

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
