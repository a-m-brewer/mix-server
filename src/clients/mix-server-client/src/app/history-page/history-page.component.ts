import {Component, OnDestroy, OnInit} from '@angular/core';
import {Subject, takeUntil} from "rxjs";
import {PlaybackSession} from "../services/repositories/models/playback-session";
import {FileExplorerFileNode} from "../main-content/file-explorer/models/file-explorer-file-node";
import {
  CurrentPlaybackSessionRepositoryService
} from "../services/repositories/current-playback-session-repository.service";
import {FileExplorerNode} from "../main-content/file-explorer/models/file-explorer-node";
import {HistoryRepositoryService} from "../services/repositories/history-repository.service";
import {NodeListItemInterface} from "../components/nodes/node-list/node-list-item/node-list-item.interface";
import {LoadingNodeStatus} from "../services/repositories/models/loading-node-status";
import {LoadingRepositoryService} from "../services/repositories/loading-repository.service";
import {
  NodeListItemChangedEvent
} from "../components/nodes/node-list/node-list-item/enums/node-list-item-changed-event";

@Component({
  selector: 'app-history-page',
  templateUrl: './history-page.component.html',
  styleUrls: ['./history-page.component.scss']
})
export class HistoryPageComponent implements OnInit, OnDestroy {
  private _unsubscribe$ = new Subject();

  public loadingStatus: LoadingNodeStatus = {loading: false, loadingIds: []};
  public sessions: PlaybackSession[] = [];
  public moreItemsAvailable: boolean = true;

  public throttle = 300;
  public scrollDistance = 1;
  public scrollUpDistance = 2;
  public selector: string = '#content-scroll-container';

  constructor(private _historyRepository: HistoryRepositoryService,
              private _loadingRepository: LoadingRepositoryService,
              private _playbackSessionRepository: CurrentPlaybackSessionRepositoryService) {
  }

  public ngOnInit(): void {
    this._historyRepository.sessions$
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(sessions => {
        this.sessions = sessions;
      });

    this._historyRepository.moreItemsAvailable$
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(moreItemsAvailable => {
        this.moreItemsAvailable = moreItemsAvailable;
      });

    this._loadingRepository.status$()
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(status => {
        this.loadingStatus = status;
      });

    this._historyRepository.loadMoreItems().then();
  }

  public ngOnDestroy(): void {
    this._unsubscribe$.next(null);
    this._unsubscribe$.complete();
  }

  public onNodeClick(event: NodeListItemChangedEvent) {
    const session = this.sessions.find(f => f.currentNode.absolutePath === event.id)

    if (!session) {
      return;
    }

    this._playbackSessionRepository.setFile(session.currentNode);
  }

  public onScrollDown() {
    this._historyRepository.loadMoreItems().then();
  }
}
