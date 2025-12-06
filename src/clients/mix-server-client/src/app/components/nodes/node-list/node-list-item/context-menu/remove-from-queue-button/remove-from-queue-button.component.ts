import {Component, forwardRef, Input, OnInit} from '@angular/core';
import {ContextMenuButton} from "../context-menu-button";
import {QueueItem} from "../../../../../../services/repositories/models/queue-item";
import {QueueSnapshotItemType} from "../../../../../../generated-clients/mix-server-clients";
import {QueueRepositoryService} from "../../../../../../services/repositories/queue-repository.service";

@Component({
    selector: 'app-remove-from-queue-button',
    templateUrl: './remove-from-queue-button.component.html',
    styleUrls: ['./remove-from-queue-button.component.scss'],
    providers: [{ provide: ContextMenuButton, useExisting: forwardRef(() => RemoveFromQueueButtonComponent) }],
    standalone: false
})
export class RemoveFromQueueButtonComponent extends ContextMenuButton {

  @Input()
  public item: QueueItem | null | undefined;

  constructor(private _queueRepository: QueueRepositoryService) {
    super();
  }

  public get disabled(): boolean {
    return !this.item ||
      this.item.itemType !== QueueSnapshotItemType.User ||
      this.item.isCurrentQueuePosition;
  }

  public removeFromQueue(): void {
    this._queueRepository.removeFromQueue(this.item);
  }
}
