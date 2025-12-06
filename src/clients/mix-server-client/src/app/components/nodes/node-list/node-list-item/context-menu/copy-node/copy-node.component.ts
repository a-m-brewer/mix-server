import {Component, forwardRef, Input, OnDestroy, OnInit} from '@angular/core';
import {MatIcon} from "@angular/material/icon";
import {MatMenuItem} from "@angular/material/menu";
import {ContextMenuButton} from "../context-menu-button";
import {FileExplorerFileNode} from "../../../../../../main-content/file-explorer/models/file-explorer-file-node";

import {CopyNodeService} from "../../../../../../services/nodes/copy-node.service";
import {Role} from "../../../../../../generated-clients/mix-server-clients";
import {Subject, takeUntil} from "rxjs";
import {RoleRepositoryService} from "../../../../../../services/repositories/role-repository.service";

@Component({
    selector: 'app-copy-node',
    imports: [
    MatIcon,
    MatMenuItem
],
    templateUrl: './copy-node.component.html',
    styleUrl: './copy-node.component.scss',
    providers: [{ provide: ContextMenuButton, useExisting: forwardRef(() => CopyNodeComponent) }]
})
export class CopyNodeComponent extends ContextMenuButton implements OnInit, OnDestroy {
  private _unsubscribe$ = new Subject<void>();

  public disabled: boolean = false;

  constructor(private _copyService: CopyNodeService,
              private _roleRepository: RoleRepositoryService) {
    super();
  }

  @Input()
  public file?: FileExplorerFileNode

  ngOnInit() {
    this._roleRepository.inRole$(Role.Administrator)
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(isAdmin => this.disabled = !isAdmin);
  }

  ngOnDestroy() {
    this._unsubscribe$.next();
    this._unsubscribe$.complete();
  }

  copyNode() {
    if (!this.file) {
      return;
    }

    this._copyService.setSourceNode(this.file, false);
  }
}
