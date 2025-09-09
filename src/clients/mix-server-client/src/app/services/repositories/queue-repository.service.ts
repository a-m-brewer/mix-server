import {Injectable} from '@angular/core';
import {
  AddToQueueCommand, QueuePositionDto, QueueRangeDto,
  RemoveFromQueueCommand
} from "../../generated-clients/mix-server-clients";
import {BehaviorSubject, combineLatestWith, map, Observable} from "rxjs";
import {QueueConverterService} from "../converters/queue-converter.service";
import {QueueSignalrClientService} from "../signalr/queue-signalr-client.service";
import {AuthenticationService} from "../auth/authentication.service";
import {QueueItem} from "./models/queue-item";
import {FileExplorerFileNode} from "../../main-content/file-explorer/models/file-explorer-file-node";
import {QueueEditFormRepositoryService} from "./queue-edit-form-repository.service";
import {QueueApiService} from "../api.service";
import {NodePathConverterService} from "../converters/node-path-converter.service";
import {RangedQueue} from "./models/ranged-queue";
import {QueuePosition} from "./models/QueuePosition";

@Injectable({
  providedIn: 'root'
})
export class QueueRepositoryService {
  private _initialLoadRequested$ = new BehaviorSubject<boolean>(false);
  private _queueBehaviourSubject$ = new BehaviorSubject<RangedQueue>(RangedQueue.Default);
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
          this.loadRange(0, this.pageSize).then();
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

  public get queue(): RangedQueue {
    return this._queueBehaviourSubject$.getValue();
  }

  public queue$(): Observable<RangedQueue> {
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

  public async loadRange(start: number, end: number): Promise<void> {
    const loadingId = `LoadQueuePage-${start}-${end}`;

    const result = await this._queueClient.request(
      loadingId,
      c => c.queue(start, end),
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
    const nextQueue = this._queueBehaviourSubject$.value.copy();
    nextQueue.items.forEach(child => {
      child.isCurrentPosition = !!position.current && child.id === position.current.id;
    });
    this._queueBehaviourSubject$.next(nextQueue);

    this._queuePositionSubject$.next(position);
  }

  private onQueueFolderChanged(position: QueuePosition): void {
    this._queueBehaviourSubject$.next(RangedQueue.Default);
    this.loadRange(0, this.pageSize)
      .then(() => this.setNextQueuePosition(position));
  }

  private initializeSignalR(): void {
    this._queueSignalRClient.queuePositionChanged$
      .subscribe(position => this.setNextQueuePosition(position));

    this._queueSignalRClient.queueFolderChanged$
      .subscribe(position => this.onQueueFolderChanged(position));
  }

  private nextQueue(dto: QueueRangeDto): void {
    const range = this._queueConverter.toQueueItemList(dto);
    const nextQueue = this._queueBehaviourSubject$.value.copy()
    nextQueue.addRange(range);

    this._queueBehaviourSubject$.next(nextQueue);
  }

  private nextQueuePosition(dto: QueuePositionDto): void {
    const position = this._queueConverter.toQueuePosition(dto);
    this.setNextQueuePosition(position);
  }
}
