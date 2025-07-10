import {Component, forwardRef, Input, OnDestroy, OnInit} from '@angular/core';
import {MatIcon} from "@angular/material/icon";
import {MatMenuItem} from "@angular/material/menu";
import {NgIf} from "@angular/common";
import {ContextMenuButton} from "../context-menu-button";
import {FileExplorerFolderNode} from "../../../../../../main-content/file-explorer/models/file-explorer-folder-node";
import {NodeCacheService} from "../../../../../../services/nodes/node-cache.service";
import {Subject, takeUntil} from "rxjs";
import {
  FileExplorerNodeRepositoryService
} from "../../../../../../services/repositories/file-explorer-node-repository.service";

@Component({
  selector: 'app-index-folder-button',
  standalone: true,
  imports: [
    MatIcon,
    MatMenuItem,
    NgIf
  ],
  templateUrl: './index-folder-button.component.html',
  styleUrl: './index-folder-button.component.scss',
  providers: [{provide: ContextMenuButton, useExisting: forwardRef(() => IndexFolderButtonComponent)}]
})
export class IndexFolderButtonComponent extends ContextMenuButton implements OnInit, OnDestroy {
  private _unsubscribe$ = new Subject<void>();

  private _scanInProgress: boolean = false;

  public nodeVar?: FileExplorerFolderNode;
  public disabled: boolean = false;

  constructor(private _nodeRepository: FileExplorerNodeRepositoryService,
              private _nodeCache: NodeCacheService) {
    super();
  }

  @Input()
  public set node(node: FileExplorerFolderNode) {
    this.nodeVar = node;
    this.disabled = this.getDisabledState();
  }

  public ngOnInit() {
    this._nodeCache.folderScanInProgress$
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe((inProgress) => {
        this._scanInProgress = inProgress;
        this.disabled = this.getDisabledState();
      });
  }

  public ngOnDestroy() {
    this._unsubscribe$.next();
    this._unsubscribe$.complete();
  }

  indexFolder() {
    if (!this.nodeVar) {
      return;
    }

    this._nodeRepository.indexFolder(this.nodeVar.path);
  }

  private getDisabledState(): boolean {
    return !this.nodeVar || !this.nodeVar.exists || this.nodeVar.disabled || this._scanInProgress;
  }
}
