import {Component, OnDestroy, OnInit} from '@angular/core';
import {MatButton} from "@angular/material/button";
import {CopyNodeService} from "../../../services/nodes/copy-node.service";
import {Subject, takeUntil} from "rxjs";
import {FileExplorerFileNode} from "../models/file-explorer-file-node";
import {NgIf} from "@angular/common";
import {FileExplorerNodeRepositoryService} from "../../../services/repositories/file-explorer-node-repository.service";
import {FileExplorerFolder} from "../models/file-explorer-folder";
import {PagedFileExplorerFolder} from "../models/paged-file-explorer-folder";

@Component({
  selector: 'app-folder-paste-form',
  standalone: true,
  imports: [
    MatButton,
    NgIf
  ],
  templateUrl: './folder-paste-form.component.html',
  styleUrl: './folder-paste-form.component.scss'
})
export class FolderPasteFormComponent implements OnInit, OnDestroy {
  private _unsubscribe$ = new Subject<void>();

  public sourceNode: FileExplorerFileNode | null = null;
  public currentFolder: PagedFileExplorerFolder | null = null;
  public loading = false;

  constructor(private _copyService: CopyNodeService,
              private _fileExplorer: FileExplorerNodeRepositoryService) {
  }

  ngOnInit() {
    this._copyService.sourceNode$
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(sourceNode => {
        this.sourceNode = sourceNode;
      });

    this._fileExplorer.currentFolder$
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(currentFolder => {
        this.currentFolder = currentFolder;
      });
  }

  ngOnDestroy() {
    this._unsubscribe$.next();
    this._unsubscribe$.complete();
  }

  async paste() {
    if (!this.sourceNode || !this.currentFolder) {
      return;
    }

    if (this._copyService.isMove &&
      this.sourceNode.parent.path.isEqual(this.currentFolder.node.path)) {
      this._copyService.resetForm();
      return;
    }

    const nameSections = this.sourceNode.nameSections;

    if (!nameSections) {
      return;
    }

    this.loading = true;

    const duplicates = this.currentFolder.flatChildren
      .map(child => {
        const childNameSections = child instanceof FileExplorerFileNode
          ? child.nameSections
          : null;

        if (!childNameSections) {
          return null;
        }

        return childNameSections.nameWithoutSuffix === nameSections.nameWithoutSuffix
          ? childNameSections
          : null;
      })
      .filter(f => f !== null);

    const maxCopyNumber = Math.max(...duplicates.map(d => d?.copyNumber ?? -1));

    let sourceNode = this.sourceNode;
    if (duplicates.length > 0) {
      sourceNode = this.sourceNode.copy();
      sourceNode.exists = false;

      const suffix = maxCopyNumber === -1 ? '' : ` (${maxCopyNumber + 1})`;
      sourceNode.path.fileName = `${nameSections.nameWithoutSuffix} - Copy${suffix}.${nameSections.extension}`
    }

    await this._copyService.pasteNode(sourceNode, false);

    this.loading = false;
  }
}
