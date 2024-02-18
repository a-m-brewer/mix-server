import {Component, OnDestroy, OnInit} from '@angular/core';
import {QueueRepositoryService} from "../services/repositories/queue-repository.service";
import {Subject, takeUntil} from "rxjs";
import {Queue} from "../services/repositories/models/queue";
import {QueueItem} from "../services/repositories/models/queue-item";
import {QueueSnapshotItemType} from "../generated-clients/mix-server-clients";
import {EditQueueFormModel} from "../services/repositories/models/edit-queue-form-model";
import {FileExplorerNodeState} from "../main-content/file-explorer/enums/file-explorer-node-state.enum";
import {FileExplorerNode} from "../main-content/file-explorer/models/file-explorer-node";
import {QueueEditFormRepositoryService} from "../services/repositories/queue-edit-form-repository.service";

@Component({
  selector: 'app-queue-page',
  templateUrl: './queue-page.component.html',
  styleUrls: ['./queue-page.component.scss']
})
export class QueuePageComponent implements OnInit, OnDestroy {
  private _unsubscribe$ = new Subject();

  protected readonly UserItemType = QueueSnapshotItemType.User;

  public queue: Queue = new Queue(null, []);
  public editQueueForm: EditQueueFormModel = new EditQueueFormModel();

  constructor(private _queueRepository: QueueRepositoryService,
              private _queueEditFormRepository: QueueEditFormRepositoryService) {
  }

  public ngOnInit(): void {
    this._queueRepository.queue$()
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(queue => {
        this.queue.unsubscribeQueueSubscriptions();
        this.queue = queue;
        this.queue.itemSelected$
          .subscribe(i =>
            this._queueEditFormRepository.updateEditForm(f => f.selectedItems[i.id] = i.file.state.selected))
      });

    this._queueEditFormRepository.editForm$
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(form => {
        this.editQueueForm = form

        this.queue.items.forEach(item => {
          if (item.file.state.isPlayingOrPaused || item.itemType !== QueueSnapshotItemType.User) {
            return;
          }

          item.file.state.selected = item.id in form.selectedItems && form.selectedItems[item.id];
          item.file.state.editing = form.editing;
        })
      });
  }

  public ngOnDestroy(): void {
    this._unsubscribe$.next(null);
    this._unsubscribe$.complete();
  }

  public onNodeClick(node: FileExplorerNode): void {
    if (node instanceof QueueItem) {
      this._queueRepository.setQueuePosition(node.id);
    }
  }
}
