import {Component, OnDestroy, OnInit} from '@angular/core';
import {MatIconButton} from "@angular/material/button";
import {MatIcon} from "@angular/material/icon";
import {NgIf} from "@angular/common";
import {FileExplorerFolder} from "../../../../main-content/file-explorer/models/file-explorer-folder";
import {LoadingNodeStatus, LoadingNodeStatusImpl} from "../../../../services/repositories/models/loading-node-status";
import {Subject, takeUntil} from "rxjs";
import {LoadingRepositoryService} from "../../../../services/repositories/loading-repository.service";
import {
  FileExplorerNodeRepositoryService
} from "../../../../services/repositories/file-explorer-node-repository.service";
import {NodeCacheService} from "../../../../services/nodes/node-cache.service";
import {PagedFileExplorerFolder} from "../../../../main-content/file-explorer/models/paged-file-explorer-folder";

@Component({
  selector: 'app-refresh-folder-button',
  standalone: true,
  imports: [
    MatIconButton,
    MatIcon,
    NgIf
  ],
  templateUrl: './refresh-folder-button.component.html',
  styleUrl: './refresh-folder-button.component.scss'
})
export class RefreshFolderButtonComponent implements OnInit, OnDestroy {
  private _unsubscribe$ = new Subject<void>();

  private _scanInProgress = false;
  private _loading = false;

  constructor(private _loadingRepository: LoadingRepositoryService,
              private _nodeCache: NodeCacheService,
              private _nodeRepository: FileExplorerNodeRepositoryService) {
  }

  public currentFolder?: PagedFileExplorerFolder | null;
  public disabled = false;

  ngOnInit() {
    this._loadingRepository.status$()
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(status => {
        this._loading = status.loading;
        this.disabled = this.getDisabled();
      });

    this._nodeRepository.currentFolder$
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(currentFolder => {
        this.currentFolder = currentFolder;
      });

    this._nodeCache.folderScanInProgress$
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(scanInProgress => {
        this._scanInProgress = scanInProgress;
        this.disabled = this.getDisabled();
      });
  }

  ngOnDestroy() {
    this._unsubscribe$.next();
    this._unsubscribe$.complete();
  }

  refreshFolder() {
    this._nodeRepository.refreshFolder();
  }

  private getDisabled(): boolean {
    return this._loading || this._scanInProgress || !this.currentFolder;
  }
}
