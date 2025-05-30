import {Component, OnDestroy, OnInit} from '@angular/core';
import {QueueRepositoryService} from "../services/repositories/queue-repository.service";
import {Subject, takeUntil} from "rxjs";
import {Queue} from "../services/repositories/models/queue";
import {QueueSnapshotItemType} from "../generated-clients/mix-server-clients";
import {EditQueueFormModel} from "../services/repositories/models/edit-queue-form-model";
import {QueueEditFormRepositoryService} from "../services/repositories/queue-edit-form-repository.service";
import {
  NodeListItemChangedEvent
} from "../components/nodes/node-list/node-list-item/interfaces/node-list-item-changed-event";
import {LoadingRepositoryService} from "../services/repositories/loading-repository.service";
import {LoadingNodeStatus, LoadingNodeStatusImpl} from "../services/repositories/models/loading-node-status";
import {AudioPlayerStateModel} from "../services/audio-player/models/audio-player-state-model";
import {AudioPlayerStateService} from "../services/audio-player/audio-player-state.service";
import {
  NodeListItemSelectedEvent
} from "../components/nodes/node-list/node-list-item/interfaces/node-list-item-selected-event";
import {SessionService} from "../services/sessions/session.service";

@Component({
  selector: 'app-queue-page',
  templateUrl: './queue-page.component.html',
  styleUrls: ['./queue-page.component.scss']
})
export class QueuePageComponent implements OnInit, OnDestroy {
  private _unsubscribe$ = new Subject();

  protected readonly UserItemType = QueueSnapshotItemType.User;

  public audioPlayerState: AudioPlayerStateModel = new AudioPlayerStateModel();
  public queue: Queue = new Queue(null, null, null, []);
  public editQueueForm: EditQueueFormModel = new EditQueueFormModel();
  public loadingStatus: LoadingNodeStatus = LoadingNodeStatusImpl.new;

  constructor(private _audioPlayerStateService: AudioPlayerStateService,
              private _loadingRepository: LoadingRepositoryService,
              private _sessionService: SessionService,
              private _queueRepository: QueueRepositoryService,
              private _queueEditFormRepository: QueueEditFormRepositoryService) {
  }

  public ngOnInit(): void {
    this._audioPlayerStateService.state$
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(state => {
        this.audioPlayerState = state;
      });

    this._queueRepository.queue$()
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(queue => {
        this.queue = queue;
      });

    this._queueEditFormRepository.editForm$
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(form => {
        this.editQueueForm = form
      });

    this._loadingRepository.status$()
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(status => {
        this.loadingStatus = status;
      });
  }

  public ngOnDestroy(): void {
    this._unsubscribe$.next(null);
    this._unsubscribe$.complete();
  }

  public onNodeClick(event: NodeListItemChangedEvent): void {
    this._sessionService.setQueuePosition(event.key);
  }

  public onNodeSelectedChanged(e: NodeListItemSelectedEvent) {
    this._queueEditFormRepository.updateEditForm(form => {
      form.selectedItems[e.key] = e.selected;
    })
  }

  protected readonly QueueSnapshotItemType = QueueSnapshotItemType;
}
