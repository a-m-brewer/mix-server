import {Component, OnDestroy, OnInit} from '@angular/core';
import {QueueRepositoryService} from "../services/repositories/queue-repository.service";
import {Subject, Subscription, takeUntil} from "rxjs";
import {Queue} from "../services/repositories/models/queue";
import {NodeListItem} from "../components/nodes/node-list/node-list-item/models/node-list-item";
import {QueueItem} from "../services/repositories/models/queue-item";
import {QueueSnapshotItemType} from "../generated-clients/mix-server-clients";
import {EditQueueFormModel} from "../services/repositories/models/edit-queue-form-model";
import {FileExplorerNodeState} from "../main-content/file-explorer/enums/file-explorer-node-state.enum";

@Component({
  selector: 'app-queue-page',
  templateUrl: './queue-page.component.html',
  styleUrls: ['./queue-page.component.scss']
})
export class QueuePageComponent implements OnInit, OnDestroy {
  private _subscriptions: Array<Subscription> = [];
  private _unsubscribe$ = new Subject();

  protected readonly UserItemType = QueueSnapshotItemType.User;

  public queue: Queue = new Queue(null, []);
  public editQueueForm: EditQueueFormModel = new EditQueueFormModel();

  constructor(private _queueRepository: QueueRepositoryService) {
  }

  public ngOnInit(): void {
    this._queueRepository.queue$()
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(queue => {
        this.queue.unsubscribeQueueSubscriptions();
        this.queue = queue;
        this.queue.itemSelected$
          .subscribe(i =>
            this._queueRepository.updateEditForm(f => f.selectedItems[i.id] = i.selected))
      });

    this._queueRepository.editForm$
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(form => {
        this.editQueueForm = form

        this.queue.items.forEach(item => {
          if (item.file.isCurrentSession || item.itemType !== QueueSnapshotItemType.User) {
            return;
          }

          item.selected = item.id in form.selectedItems && form.selectedItems[item.id];

          item.state = form.editing ? FileExplorerNodeState.Editing : FileExplorerNodeState.None;
        })
      });
  }

  public ngOnDestroy(): void {
    this._unsubscribe$.next(null);
    this._unsubscribe$.complete();
  }

  public onNodeClick(node: NodeListItem): void {
    if (node instanceof QueueItem) {
      this._queueRepository.setQueuePosition(node.id);
    }
  }
}
