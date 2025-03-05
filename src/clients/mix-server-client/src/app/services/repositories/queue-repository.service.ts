import {Injectable} from '@angular/core';
import {
  AddToQueueCommand,
  QueueSnapshotDto,
  RemoveFromQueueCommand
} from "../../generated-clients/mix-server-clients";
import {BehaviorSubject, map, Observable} from "rxjs";
import {Queue} from "./models/queue";
import {QueueConverterService} from "../converters/queue-converter.service";
import {QueueSignalrClientService} from "../signalr/queue-signalr-client.service";
import {AuthenticationService} from "../auth/authentication.service";
import {QueueItem} from "./models/queue-item";
import {FileExplorerFileNode} from "../../main-content/file-explorer/models/file-explorer-file-node";
import {QueueEditFormRepositoryService} from "./queue-edit-form-repository.service";
import {NodeCacheService} from "../nodes/node-cache.service";
import {QueueApiService} from "../api.service";

@Injectable({
  providedIn: 'root'
})
export class QueueRepositoryService {
  private _queueBehaviourSubject$ = new BehaviorSubject<Queue>(new Queue(null, null, null, []));


  constructor(private _nodeCache: NodeCacheService,
              private _queueConverter: QueueConverterService,
              private _queueSignalRClient: QueueSignalrClientService,
              private _queueClient: QueueApiService,
              private _authenticationService: AuthenticationService,
              private _queueEditFormRepository: QueueEditFormRepositoryService) {
    this._authenticationService.connected$
      .subscribe(connected => {
        if (connected) {
          this._queueClient.request('GetQueue', client => client.queue(), 'Failed to get queue', [404])
            .then(result => result.success(dto => this.nextQueue(dto)))
        }
      });

    this.initializeSignalR();
  }

  public get queue(): Queue {
    return this._queueBehaviourSubject$.getValue();
  }

  public queue$(): Observable<Queue> {
    return this._queueBehaviourSubject$.asObservable();
  }

  public queuePosition$(): Observable<QueueItem | null | undefined> {
    return this._queueBehaviourSubject$
      .pipe(map(q => q.items.find(f => f.id === q.currentQueuePosition)))
  }

  public previousQueueItem$(): Observable<QueueItem | null | undefined> {
    return this._queueBehaviourSubject$
      .pipe(map(q => this.getPreviousQueueItem(q)));
  }

  public get previousQueueItem(): QueueItem | null | undefined {
    return this.getPreviousQueueItem(this.queue);
  }

  private getPreviousQueueItem(queue: Queue): QueueItem | null | undefined {
    const currentItemIndex = queue.items.findIndex(f => f.id === queue.currentQueuePosition);

    return currentItemIndex == -1 || currentItemIndex <= 0
      ? null
      : queue.items[currentItemIndex - 1];
  }

  public nextQueueItem$(): Observable<QueueItem | null | undefined> {
    return this._queueBehaviourSubject$
      .pipe(map(q => this.getNextQueueItem(q)));
  }

  public get nextQueueItem(): QueueItem | null | undefined {
    return this.getNextQueueItem(this.queue);
  }

  private getNextQueueItem(queue: Queue): QueueItem | null | undefined {
    const currentItemIndex = queue.items.findIndex(f => f.id === queue.currentQueuePosition);

    if (currentItemIndex == -1) {
      return null
    }

    const nextIndex = currentItemIndex + 1;
    if (nextIndex >= queue.items.length) {
      return null;
    }

    return queue.items[currentItemIndex + 1];
  }

  public addToQueue(file: FileExplorerFileNode): void {
    this._queueClient.request(file.absolutePath,
      client => client.addToQueue(new AddToQueueCommand({
        absoluteFolderPath: file.parent.absolutePath ?? '',
        fileName: file.name
      })), 'Failed to add item to queue')
      .then(result => result.success(dto => this.nextQueue(dto)));
  }

  public removeFromQueue(item: QueueItem | null | undefined): void {
    if (!item) {
      return;
    }

    this._queueClient.request(item.id, client => client.removeFromQueue(item.id), 'Failed to remove item from queue')
      .then(result => result.success(dto => this.nextQueue(dto)));
  }

  public removeRangeFromQueue(queueItems: Array<string>): void {
    if (queueItems.length === 0) {
      return;
    }

    this._queueClient.request(queueItems,
      client => client.removeFromQueue2(new RemoveFromQueueCommand({queueItems})), 'Failed to remove items from queue')
      .then(result => result.success(dto => {
        this.nextQueue(dto);
        this._queueEditFormRepository.updateEditForm(f => f.selectedItems = {});
      }));
  }

  public setNextQueue(queue: Queue) {
    const folders = [...new Set(queue.items.map(item => item.file.parent.absolutePath))];
    folders.forEach(folder => {
      void this._nodeCache.loadDirectory(folder)
    })

    this._queueBehaviourSubject$.value.destroy();

    this._queueBehaviourSubject$.next(queue);
  }

  private initializeSignalR(): void {
    this._queueSignalRClient.queue$()
      .subscribe({
        next: updatedQueue => {
          this.setNextQueue(updatedQueue);
        }
      })
  }

  private nextQueue(dto: QueueSnapshotDto): void {
    this.setNextQueue(this._queueConverter.fromDto(dto));
  }
}
