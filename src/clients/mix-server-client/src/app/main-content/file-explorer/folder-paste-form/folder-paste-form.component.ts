import {Component, OnDestroy, OnInit} from '@angular/core';
import {MatButton} from "@angular/material/button";
import {CopyNodeService} from "../../../services/nodes/copy-node.service";
import {firstValueFrom, Subject, takeUntil} from "rxjs";
import {FileExplorerFileNode} from "../models/file-explorer-file-node";
import {NgIf} from "@angular/common";
import {FileExplorerNodeRepositoryService} from "../../../services/repositories/file-explorer-node-repository.service";
import {FileExplorerFolderNode} from "../models/file-explorer-folder-node";
import {FileExplorerFolder} from "../models/file-explorer-folder";
import {MatDialog} from "@angular/material/dialog";
import {
  ConfirmationDialogComponent
} from "../../../components/dialogs/confirmation-dialog/confirmation-dialog.component";

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
  public currentFolder: FileExplorerFolder | null = null;

  constructor(private _copyService: CopyNodeService,
              private _dialog: MatDialog,
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

    const nameSections = this.sourceNode.nameSections;

    if (!nameSections) {
      return;
    }

    const duplicates = this.currentFolder.children
      .filter(child =>
        child instanceof FileExplorerFileNode &&
        child.nameSections &&
        child.nameSections.nameWithoutSuffix === nameSections.nameWithoutSuffix)
      .length;

    let sourceNode = this.sourceNode;
    if (duplicates > 0) {
      sourceNode = this.sourceNode.copy();
      sourceNode.exists = false;

      const suffix = duplicates === 1 ? '': ` (${duplicates})`;
      sourceNode.name = `${nameSections.nameWithoutSuffix} - Copy${suffix}.${nameSections.extension}`
    }

    await this._copyService.pasteNode(sourceNode, false);
  }
}
