import {Component, forwardRef, Input} from '@angular/core';
import {
  FileExplorerNodeRepositoryService
} from "../../../../../../services/repositories/file-explorer-node-repository.service";
import {FileExplorerFolderNode} from "../../../../../../main-content/file-explorer/models/file-explorer-folder-node";
import {ContextMenuButton} from "../context-menu-button";

@Component({
    selector: 'app-open-location-button',
    templateUrl: './open-location-button.component.html',
    styleUrls: ['./open-location-button.component.scss'],
    providers: [{ provide: ContextMenuButton, useExisting: forwardRef(() => OpenLocationButtonComponent) }],
    standalone: false
})
export class OpenLocationButtonComponent extends ContextMenuButton {
  constructor(private _nodeRepository: FileExplorerNodeRepositoryService) {
    super();
  }

  @Input()
  public folder?: FileExplorerFolderNode | null;

  public get disabled(): boolean {
    return !this.folder || this.folder.disabled;
  }

  public changeDirectory(): void {
    if (!this.folder) {
      return;
    }

    this._nodeRepository.changeDirectory(this.folder);
  }
}
