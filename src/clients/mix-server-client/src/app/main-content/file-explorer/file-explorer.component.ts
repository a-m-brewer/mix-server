import {Component, OnDestroy, OnInit} from '@angular/core';
import {FileExplorerNode} from "./models/file-explorer-node";
import {FileExplorerFolderNode} from "./models/file-explorer-folder-node";
import {FileExplorerFileNode} from "./models/file-explorer-file-node";
import {FileExplorerNodeRepositoryService} from "../../services/repositories/file-explorer-node-repository.service";
import {Subject, takeUntil} from "rxjs";
import {CurrentPlaybackSessionRepositoryService} from "../../services/repositories/current-playback-session-repository.service";
import {NodeListItem} from "../../components/nodes/node-list/node-list-item/models/node-list-item";

@Component({
  selector: 'app-file-explorer',
  templateUrl: './file-explorer.component.html',
  styleUrls: ['./file-explorer.component.scss']
})
export class FileExplorerComponent implements OnInit, OnDestroy {
  private _unsubscribe$ = new Subject();

  public nodes: FileExplorerNode[] = []
  public currentFolder?: FileExplorerFolderNode | null;
  public nodeRepositoryLoading: boolean = false;
  public playbackSessionLoading: boolean = false;

  constructor(private _nodeRepository: FileExplorerNodeRepositoryService,
              private _playbackSessionRepository: CurrentPlaybackSessionRepositoryService) {
  }

  public ngOnInit(): void {
    this._nodeRepository.getCurrentLevelNodes$()
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(remoteNodes => {
        this.nodes = [...remoteNodes];
      });

    this._nodeRepository.currentFolder$
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(currentFolder => {
        this.currentFolder = currentFolder;
      });

    this._nodeRepository.loading$
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(loading => {
        this.nodeRepositoryLoading = loading;
      })

    this._playbackSessionRepository.loading$
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(loading => {
        this.playbackSessionLoading = loading;
      })
  }

  public ngOnDestroy(): void {
    this._unsubscribe$.next(null);
    this._unsubscribe$.complete();
  }

  public onNodeClick(node: NodeListItem): void {
    if (node instanceof FileExplorerFolderNode) {
      this.onFolderClick(node as FileExplorerFolderNode);
    }

    if (node instanceof FileExplorerFileNode) {
      this.onFileClick(node as FileExplorerFileNode);
    }
  }
  private onFolderClick(folder: FileExplorerFolderNode): void {
    this._nodeRepository.changeDirectory(folder);
  }

  private onFileClick(file: FileExplorerFileNode): void {
    this._playbackSessionRepository.setFile(file);
  }
}
