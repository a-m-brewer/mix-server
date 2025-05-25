import {Component, OnDestroy, OnInit} from '@angular/core';
import {FileExplorerFolderNode} from "./models/file-explorer-folder-node";
import {FileExplorerFileNode} from "./models/file-explorer-file-node";
import {FileExplorerNodeRepositoryService} from "../../services/repositories/file-explorer-node-repository.service";
import {Subject, takeUntil} from "rxjs";
import {CurrentPlaybackSessionRepositoryService} from "../../services/repositories/current-playback-session-repository.service";
import {FileExplorerFolder} from "./models/file-explorer-folder";
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

@Component({
  selector: 'app-file-explorer',
  templateUrl: './file-explorer.component.html',
  styleUrls: ['./file-explorer.component.scss']
})
export class FileExplorerComponent implements OnInit, OnDestroy {
  private _unsubscribe$ = new Subject();

  public audioPlayerState: AudioPlayerStateModel = new AudioPlayerStateModel();
  public currentFolder: FileExplorerFolder = FileExplorerFolder.Default;
  public loadingStatus: LoadingNodeStatus = LoadingNodeStatusImpl.new;
  public isAdmin: boolean = false;

  constructor(private _audioPlayerStateService: AudioPlayerStateService,
              private _loadingRepository: LoadingRepositoryService,
              private _nodeRepository: FileExplorerNodeRepositoryService,
              private _roleRepository: RoleRepositoryService,
              private _sessionService: SessionService) {
  }

  public ngOnInit(): void {
    this._audioPlayerStateService.state$
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(state => {
        this.audioPlayerState = state;
      });

    this._nodeRepository.currentFolder$
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(currentFolder => {
        this.currentFolder = currentFolder;
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
      })
  }

  public ngOnDestroy(): void {
    this._unsubscribe$.next(null);
    this._unsubscribe$.complete();
  }

  public onNodeClick(event: NodeListItemChangedEvent): void {
    if (this.currentFolder.node.parent && event.key === this.currentFolder.node.parent.path.key) {
      this.onFolderClick(this.currentFolder.node.parent);
      return;
    }

    const childNode = this.currentFolder.children.find(s => s.path.key === event.key);

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

  protected readonly FileExplorerFileNode = FileExplorerFileNode;
}
