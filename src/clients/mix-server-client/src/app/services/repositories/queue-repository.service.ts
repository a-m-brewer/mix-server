import {Injectable} from '@angular/core';
import {
  AddToQueueCommand, QueuePageDto, QueuePositionDto,
  RemoveFromQueueCommand
} from "../../generated-clients/mix-server-clients";
import {BehaviorSubject, combineLatestWith, map, Observable} from "rxjs";
import {QueueConverterService} from "../converters/queue-converter.service";
import {QueueSignalrClientService} from "../signalr/queue-signalr-client.service";
import {AuthenticationService} from "../auth/authentication.service";
import {QueueItem} from "./models/queue-item";
import {FileExplorerFileNode} from "../../main-content/file-explorer/models/file-explorer-file-node";
import {QueueEditFormRepositoryService} from "./queue-edit-form-repository.service";
import {NodeCacheService} from "../nodes/node-cache.service";
import {QueueApiService} from "../api.service";
import {NodePathConverterService} from "../converters/node-path-converter.service";
import {PagedQueue} from "./models/paged-queue";
import {QueuePosition} from "./models/QueuePosition";

@Injectable({
  providedIn: 'root'
})
export class QueueRepositoryService {
  private _initialLoadRequested$ = new BehaviorSubject<boolean>(false);
  private _queueBehaviourSubject$ = new BehaviorSubject<PagedQueue>(PagedQueue.Default);
  private _queuePositionSubject$ = new BehaviorSubject(QueuePosition.Default);

  constructor(private _authenticationService: AuthenticationService,
              private _nodePathConverter: NodePathConverterService,
              private _queueConverter: QueueConverterService,
              private _queueSignalRClient: QueueSignalrClientService,
              private _queueClient: QueueApiService,
              private _queueEditFormRepository: QueueEditFormRepositoryService) {
    this._authenticationService.connected$
      .pipe(combineLatestWith(this._initialLoadRequested$))
      .subscribe(([connected, initialLoadRequested]) => {
        if (connected && initialLoadRequested) {
          this.loadPage(0).then();
        }
      });

    this._authenticationService.connected$
      .subscribe(connected => {
        if (connected) {
          this.reloadCurrentPosition().then();
        }
      })

    this.initializeSignalR();
  }

  public pageSize: number = 25;

  public get queue(): PagedQueue {
    return this._queueBehaviourSubject$.getValue();
  }

  public queue$(): Observable<PagedQueue> {
    return this._queueBehaviourSubject$.asObservable();
  }

  public queuePosition$(): Observable<QueueItem | null | undefined> {
    return this._queuePositionSubject$
      .pipe(map(q => q.current));
  }

  public previousQueueItem$(): Observable<QueueItem | null | undefined> {
    return this._queuePositionSubject$
      .pipe(map(q => q.previous));
  }

  public get previousQueueItem(): QueueItem | null | undefined {
    return this._queuePositionSubject$.getValue().previous;
  }

  public nextQueueItem$(): Observable<QueueItem | null | undefined> {
    return this._queuePositionSubject$
      .pipe(map(q => q.next));
  }

  public get nextQueueItem(): QueueItem | null | undefined {
    return this._queuePositionSubject$.getValue().next;
  }

  public requestInitialLoad(): void {
    if (this._initialLoadRequested$.value) {
      return;
    }

    this._initialLoadRequested$.next(true);
  }

  public async loadPage(pageIndex: number): Promise<void> {
    const loadingId = `LoadQueuePage-${pageIndex}`;

    const result = await this._queueClient.request(
      loadingId,
      c => c.queue(pageIndex, this.pageSize),
      'Failed to fetch queue',
      {
        validStatusCodes: [404]
      }
    );

    if (!result.result) {
      return;
    }

    this.nextQueue(result.result);
  }

  public async reloadCurrentPosition(): Promise<void> {
    const result = await this._queueClient.request(
      'QueuePosition',
      c => c.getQueuePosition(),
      'Failed to fetch queue position'
    );

    if (!result.result) {
      return;
    }

    this.nextQueuePosition(result.result);
  }

  public addToQueue(file: FileExplorerFileNode): void {
    this._queueClient.request(file.path.key,
      client => client.addToQueue(new AddToQueueCommand({
        nodePath: this._nodePathConverter.toRequestDto(file.path)
      })), 'Failed to add item to queue')
      .then(result => result.success(dto => this.nextQueuePosition(dto)));
  }

  public removeFromQueue(item: QueueItem | null | undefined): void {
    if (!item) {
      return;
    }

    this._queueClient.request(item.id, client => client.removeFromQueue(item.id), 'Failed to remove item from queue')
      .then(result => result.success(dto => this.nextQueuePosition(dto)));
  }

  public removeRangeFromQueue(queueItems: Array<string>): void {
    if (queueItems.length === 0) {
      return;
    }

    this._queueClient.request(queueItems,
      client => client.removeFromQueue2(new RemoveFromQueueCommand({queueItems})), 'Failed to remove items from queue')
      .then(result => result.success(dto => {
        this.nextQueuePosition(dto);
        this._queueEditFormRepository.updateEditForm(f => f.selectedItems = {});
      }));
  }

  public setNextQueuePosition(position: QueuePosition) {
    this._queuePositionSubject$.next(position);
  }

  private initializeSignalR(): void {
  }

  private nextQueue(dto: QueuePageDto): void {
    const page = this._queueConverter.toPagedQueuePage(dto);
    const nextQueue = this._queueBehaviourSubject$.value.copy()
    nextQueue.addPage(page.pageIndex, page.children);

    this._queueBehaviourSubject$.next(nextQueue);
  }

  private nextQueuePosition(dto: QueuePositionDto): void {
    const position = this._queueConverter.toQueuePosition(dto);
    this.setNextQueuePosition(position);
  }
}
