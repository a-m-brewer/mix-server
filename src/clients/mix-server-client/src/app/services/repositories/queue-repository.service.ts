import {Injectable} from '@angular/core';
import {QueueClient} from "../../generated-clients/mix-server-clients";
import {BehaviorSubject, map, Observable} from "rxjs";
import {Queue} from "./models/queue";
import {QueueConverterService} from "../converters/queue-converter.service";
import {
  AddToQueueCommand,
  ProblemDetails,
  RemoveFromQueueCommand,
  SetQueuePositionCommand
} from "../../generated-clients/mix-server-clients";
import {ToastService} from "../toasts/toast-service";
import {QueueSignalrClientService} from "../signalr/queue-signalr-client.service";
import {AuthenticationService} from "../auth/authentication.service";
import {AudioPlayerStateService} from "../audio-player/audio-player-state.service";
import {QueueItem} from "./models/queue-item";
import {EditQueueFormModel} from "./models/edit-queue-form-model";
import {FileExplorerFileNode} from "../../main-content/file-explorer/models/file-explorer-file-node";

@Injectable({
  providedIn: 'root'
})
export class QueueRepositoryService {
  private _queueBehaviourSubject$ = new BehaviorSubject<Queue>(new Queue(null, []));
  private _editQueueFormBehaviourSubject$ = new BehaviorSubject<EditQueueFormModel>(new EditQueueFormModel());


  constructor(private _audioPlayerState: AudioPlayerStateService,
              private _queueConverter: QueueConverterService,
              private _queueSignalRClient: QueueSignalrClientService,
              private _queueClient: QueueClient,
              private _toastService: ToastService,
              private _authenticationService: AuthenticationService) {
    this._authenticationService.connected$
      .subscribe(connected => {
        if (connected) {
          this._queueClient.queue()
            .subscribe({
              next: dto => {
                const queue = this._queueConverter.fromDto(dto);

                this.nextQueue(queue);
              },
              error: err => {
                if ((err as ProblemDetails)?.status !== 404) {
                  this._toastService.logServerError(err, 'Failed to fetch current session');
                }
              }
            });
        }
      });

    this.initializeSignalR();
  }

  public queue$(): Observable<Queue> {
    return this._queueBehaviourSubject$.asObservable();
  }

  public get queue(): Queue {
    return this._queueBehaviourSubject$.getValue();
  }

  public queuePosition$(): Observable<QueueItem | null | undefined> {
    return this._queueBehaviourSubject$
      .pipe(map(q => q.items.find(f => f.id === q.currentQueuePosition)))
  }

  public previousQueueItem$(): Observable<QueueItem | null | undefined> {
    return this._queueBehaviourSubject$
      .pipe(map(q => {
        const currentItemIndex = q.items.findIndex(f => f.id === q.currentQueuePosition);

        return currentItemIndex == -1 || currentItemIndex <= 0
          ? null
          : q.items[currentItemIndex - 1];
      }))
  }

  public nextQueueItem$(): Observable<QueueItem | null | undefined> {
    return this._queueBehaviourSubject$
      .pipe(map(q => {
        const currentItemIndex = q.items.findIndex(f => f.id === q.currentQueuePosition);

        if (currentItemIndex == -1) {
          return null
        }

        const nextIndex = currentItemIndex + 1;
        if (nextIndex >= q.items.length) {
          return null;
        }

        return q.items[currentItemIndex + 1];
      }));
  }

  public get editForm$(): Observable<EditQueueFormModel> {
    return this._editQueueFormBehaviourSubject$.asObservable();
  }

  public get editForm(): EditQueueFormModel {
    return this._editQueueFormBehaviourSubject$.getValue();
  }

  public updateEditForm(update: (form: EditQueueFormModel) => void): void {
    const form = EditQueueFormModel.copy(this.editForm);

    update(form);

    this._editQueueFormBehaviourSubject$.next(form);
  }

  public addToQueue(file: FileExplorerFileNode): void {
    this._queueClient.addToQueue(new AddToQueueCommand({
      absoluteFolderPath: file.parent.absolutePath ?? '',
      fileName: file.name
    })).subscribe({
      error: err => this._toastService.logServerError(err, 'Failed to add item to queue')
    });
  }

  public removeFromQueue(item: QueueItem | null | undefined): void {
    if (!item) {
      return;
    }

    this._queueClient.removeFromQueue(item.id)
      .subscribe({
        error: err => this._toastService.logServerError(err, 'Failed to remove item from queue')
      });
  }

  public removeRangeFromQueue(queueItems: Array<string>) {
    if (queueItems.length === 0) {
      return;
    }

    this._queueClient.removeFromQueue2(new RemoveFromQueueCommand({queueItems}))
      .subscribe({
        next: () => this.updateEditForm(f => f.selectedItems = {}),
        error: err => this._toastService.logServerError(err, 'Failed to remove items from queue')
      });
  }

  public setQueuePosition(queueItemId: string): void {
    this._queueClient.setQueuePosition(new SetQueuePositionCommand({
      queueItemId
    })).subscribe({
      error: err => this._toastService.logServerError(err, 'Failed to set queue position')
    });
  }

  private initializeSignalR(): void {
    this._queueSignalRClient.queue$()
      .subscribe({
        next: updatedQueue => {
          this.nextQueue(updatedQueue);
        }
      })
  }

  private nextQueue(queue: Queue): void {
    this._queueBehaviourSubject$.getValue().unsubscribeQueueSubscriptions();
    this._queueBehaviourSubject$.next(queue);
  }
}
