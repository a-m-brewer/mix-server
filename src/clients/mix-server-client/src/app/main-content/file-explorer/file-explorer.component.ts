import {Component, OnDestroy, OnInit} from '@angular/core';
import {FileExplorerFolderNode} from "./models/file-explorer-folder-node";
import {FileExplorerFileNode} from "./models/file-explorer-file-node";
import {FileExplorerNodeRepositoryService} from "../../services/repositories/file-explorer-node-repository.service";
import {Subject, takeUntil} from "rxjs";
import {LoadingNodeStatus, LoadingNodeStatusImpl} from "../../services/repositories/models/loading-node-status";
import {LoadingRepositoryService} from "../../services/repositories/loading-repository.service";
import {
  NodeListItemChangedEvent
} from "../../components/nodes/node-list/node-list-item/interfaces/node-list-item-changed-event";
import {AudioPlayerStateService} from "../../services/audio-player/audio-player-state.service";
import {AudioPlayerStateModel} from "../../services/audio-player/models/audio-player-state-model";
import {SessionService} from "../../services/sessions/session.service";
import {RoleRepositoryService} from "../../services/repositories/role-repository.service";
import {Role} from "../../generated-clients/mix-server-clients";
import {FileExplorerDataSource} from "./file-explorer-data-source";
import {NodeApiService} from "../../services/api.service";
import {FileExplorerNodeConverterService} from "../../services/converters/file-explorer-node-converter.service";
import {FolderSignalrClientService} from "../../services/signalr/folder-signalr-client.service";
import {PlaybackDeviceRepositoryService} from "../../services/repositories/playback-device-repository.service";
import {FolderSort} from "./models/folder-sort";

@Component({
    selector: 'app-file-explorer',
    templateUrl: './file-explorer.component.html',
    styleUrls: ['./file-explorer.component.scss'],
    standalone: false
})
export class FileExplorerComponent implements OnInit, OnDestroy {
  private _unsubscribe$ = new Subject();

  public audioPlayerState: AudioPlayerStateModel = new AudioPlayerStateModel();
  public folderNode: FileExplorerFolderNode = FileExplorerFolderNode.Default;
  public folderSort: FolderSort = FolderSort.Default;
  public loadingStatus: LoadingNodeStatus = LoadingNodeStatusImpl.new;
  public isAdmin: boolean = false;
  public dataSource!: FileExplorerDataSource;

  constructor(private _audioPlayerStateService: AudioPlayerStateService,
              private _fileExplorerNodeConverter: FileExplorerNodeConverterService,
              private _folderSignalRClient: FolderSignalrClientService,
              private _loadingRepository: LoadingRepositoryService,
              private _nodeApiService: NodeApiService,
              private _nodeRepository: FileExplorerNodeRepositoryService,
              private _playbackDeviceService: PlaybackDeviceRepositoryService,
              private _roleRepository: RoleRepositoryService,
              private _sessionService: SessionService) {
  }

  public ngOnInit(): void {
    this.dataSource = new FileExplorerDataSource(
      (rootPath, relativePath, start, end) => this._fetchRange(rootPath, relativePath, start, end)
    );

    this._audioPlayerStateService.state$
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(state => {
        this.audioPlayerState = state;
      });

    this._nodeRepository.currentFolderPath$
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(path => {
        if (path) {
          this.dataSource.loadFolder(path).then();
        }
      });

    this.dataSource.folderNode$
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(node => {
        this.folderNode = node;
      });

    this.dataSource.sort$
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(sort => {
        this.folderSort = sort;
      });

    this._loadingRepository.status$()
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(status => {
        this.loadingStatus = status;
      });

    this._roleRepository.inRole$(Role.Administrator)
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(isAdmin => {
        this.isAdmin = isAdmin;
      });

    // SignalR: nodeUpdated - reset if parent matches current folder
    this._folderSignalRClient.nodeUpdated$()
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(event => {
        if (event.node.parent?.path.key === this.folderNode.path.key) {
          this.dataSource.reset().then();
        }
      });

    // SignalR: nodeDeleted - reset if parent matches current folder
    this._folderSignalRClient.nodeDeleted$()
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(event => {
        if (event.parent.path.key === this.folderNode.path.key) {
          this.dataSource.reset().then();
        }
      });

    // SignalR: folderSorted - reset if folder matches current folder
    this._folderSignalRClient.folderSorted$()
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(folder => {
        if (folder.node.path.key === this.folderNode.path.key) {
          this.dataSource.reset().then();
        }
      });

    // SignalR: folderRefreshed - reset if folder matches current folder
    this._folderSignalRClient.folderRefreshed$()
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(folder => {
        if (folder.node.path.key === this.folderNode.path.key) {
          this.dataSource.reset().then();
        }
      });

    // SignalR: mediaInfoUpdated - update metadata in-place
    this._folderSignalRClient.mediaInfoUpdated$()
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(event => {
        for (const item of event.mediaInfo) {
          if (item.nodePath.parent.key === this.folderNode.path.key) {
            this.dataSource.updateNodeMetadata(item.nodePath.key, node => {
              if (node instanceof FileExplorerFileNode) {
                node.metadata.mediaInfo = item.info;
              }
            });
          }
        }
      });

    // SignalR: mediaInfoRemoved - clear metadata in-place
    this._folderSignalRClient.mediaInfoRemoved$()
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(event => {
        for (const nodePath of event.nodePaths) {
          if (nodePath.parent.key === this.folderNode.path.key) {
            this.dataSource.updateNodeMetadata(nodePath.key, node => {
              if (node instanceof FileExplorerFileNode) {
                node.metadata.mediaInfo = null;
              }
            });
          }
        }
      });

    // Playback device changes - update canPlay on cached nodes
    this._playbackDeviceService.requestPlaybackDevice$
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(device => {
        this.dataSource.updateAllNodes(node => {
          if (node instanceof FileExplorerFileNode) {
            node.updateCanPlay(device);
          }
        });
      });
  }

  public ngOnDestroy(): void {
    this._unsubscribe$.next(null);
    this._unsubscribe$.complete();
    this.dataSource.disconnect();
  }

  public onNodeClick(event: NodeListItemChangedEvent): void {
    if (this.folderNode.parent && event.key === this.folderNode.parent.path.key) {
      this.onFolderClick(this.folderNode.parent);
      return;
    }

    const childNode = this.dataSource.currentData.find(n => n?.path.key === event.key);

    if (childNode instanceof FileExplorerFolderNode) {
      this.onFolderClick(childNode);
    }

    if (childNode instanceof FileExplorerFileNode) {
      this.onFileClick(childNode);
    }
  }

  private onFolderClick(folder: FileExplorerFolderNode): void {
    this._nodeRepository.changeDirectory(folder);
  }

  private onFileClick(file: FileExplorerFileNode): void {
    this._sessionService.setFile(file);
  }

  private async _fetchRange(rootPath: string, relativePath: string, start: number, end: number) {
    const result = await this._nodeApiService.request(
      `file-explorer-${start}-${end}`,
      client => client.getNode(rootPath, relativePath, start, end),
      'Error loading directory',
      {triggerLoading: start === 0}
    );

    if (!result.result) {
      return {children: [], totalCount: 0, node: FileExplorerFolderNode.Default, sort: FolderSort.Default};
    }

    const folder = this._fileExplorerNodeConverter.fromFileExplorerFolder(result.result);

    return {
      children: folder.children,
      totalCount: result.result.totalCount,
      node: folder.node,
      sort: folder.sort
    };
  }

  protected readonly FileExplorerFileNode = FileExplorerFileNode;
}
