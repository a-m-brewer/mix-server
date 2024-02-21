import {Injectable} from '@angular/core';
import {QueueClient} from "../../generated-clients/mix-server-clients";
import {BehaviorSubject, firstValueFrom, map, Observable} from "rxjs";
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
import {QueueItem} from "./models/queue-item";
import {FileExplorerFileNode} from "../../main-content/file-explorer/models/file-explorer-file-node";
import {QueueEditFormRepositoryService} from "./queue-edit-form-repository.service";
import {LoadingRepositoryService} from "./loading-repository.service";

@Injectable({
  providedIn: 'root'
})
export class QueueRepositoryService {
  private _queueBehaviourSubject$ = new BehaviorSubject<Queue>(new Queue(null, []));


  constructor(private _loadingRepository: LoadingRepositoryService,
              private _queueConverter: QueueConverterService,
              private _queueSignalRClient: QueueSignalrClientService,
              private _queueClient: QueueClient,
              private _toastService: ToastService,
              private _authenticationService: AuthenticationService,
              private _queueEditFormRepository: QueueEditFormRepositoryService) {
    this._authenticationService.connected$
      .subscribe(connected => {
        if (connected) {
          this._loadingRepository.startLoading();
          firstValueFrom(this._queueClient.queue())
            .then(dto => {
              const queue = this._queueConverter.fromDto(dto);

              this.nextQueue(queue);
            })
            .catch(err => {
              if ((err as ProblemDetails)?.status !== 404) {
                this._toastService.logServerError(err, 'Failed to fetch current session');
              }
            })
            .finally(() => this._loadingRepository.stopLoading());
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

  public addToQueue(file: FileExplorerFileNode): void {
    this._loadingRepository.startLoadingId(file.absolutePath);
    firstValueFrom(this._queueClient.addToQueue(new AddToQueueCommand({
      absoluteFolderPath: file.parent.absolutePath ?? '',
      fileName: file.name
    })))
      .catch(err => this._toastService.logServerError(err, 'Failed to add item to queue'))
      .finally(() => this._loadingRepository.stopLoadingId(file.absolutePath));
  }

  public removeFromQueue(item: QueueItem | null | undefined): void {
    if (!item) {
      return;
    }

    this._loadingRepository.startLoadingId(item.id);
    firstValueFrom(this._queueClient.removeFromQueue(item.id))
      .catch(err => this._toastService.logServerError(err, 'Failed to remove item from queue'))
      .finally(() => this._loadingRepository.stopLoadingId(item.id));
  }

  public removeRangeFromQueue(queueItems: Array<string>): void {
    if (queueItems.length === 0) {
      return;
    }

    this._loadingRepository.startLoadingIds(queueItems);

    firstValueFrom(this._queueClient.removeFromQueue2(new RemoveFromQueueCommand({queueItems})))
      .then(() => this._queueEditFormRepository.updateEditForm(f => f.selectedItems = {}))
      .catch(err => this._toastService.logServerError(err, 'Failed to remove items from queue'))
      .then(() => this._loadingRepository.stopLoadingIds(queueItems));
  }

  public setQueuePosition(queueItemId: string): void {
    this._loadingRepository.startLoadingId(queueItemId);
    firstValueFrom(this._queueClient.setQueuePosition(new SetQueuePositionCommand({
      queueItemId
    })))
      .catch(err => this._toastService.logServerError(err, 'Failed to set queue position'))
      .finally(() => this._loadingRepository.stopLoadingId(queueItemId));
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
    this._queueBehaviourSubject$.next(queue);
  }
}
