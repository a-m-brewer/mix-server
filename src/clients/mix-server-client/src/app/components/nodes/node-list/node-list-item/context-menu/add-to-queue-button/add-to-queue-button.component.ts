import {Component, forwardRef, Input} from '@angular/core';
import {ContextMenuButton} from "../context-menu-button";
import {FileExplorerFileNode} from "../../../../../../main-content/file-explorer/models/file-explorer-file-node";
import {FileExplorerNode} from "../../../../../../main-content/file-explorer/models/file-explorer-node";
import {QueueRepositoryService} from "../../../../../../services/repositories/queue-repository.service";

@Component({
  selector: 'app-add-to-queue-button',
  templateUrl: './add-to-queue-button.component.html',
  styleUrls: ['./add-to-queue-button.component.scss'],
  providers: [{provide: ContextMenuButton, useExisting: forwardRef(() => AddToQueueButtonComponent)}]
})
export class AddToQueueButtonComponent extends ContextMenuButton {

  constructor(private _queueRepository: QueueRepositoryService) {
    super();
  }

  public fileVar?: FileExplorerFileNode;

  public disabled: boolean = false;

  @Input()
  public set file(file: FileExplorerFileNode) {
    this.fileVar = file;
    this.disabled = !file || file.playbackDisabled;
  }

  public addToQueue(): void {
    if (!this.fileVar) {
      return;
    }

    this._queueRepository.addToQueue(this.fileVar)
  }
}
