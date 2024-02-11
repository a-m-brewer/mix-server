import {Component, ElementRef, OnDestroy, OnInit, ViewChild} from '@angular/core';
import {Subject, takeUntil} from "rxjs";
import {CurrentPlaybackSessionRepositoryService} from "../../../services/repositories/current-playback-session-repository.service";
import {FileExplorerNodeRepositoryService} from "../../../services/repositories/file-explorer-node-repository.service";
import {FileExplorerFolderNode} from "../../../main-content/file-explorer/models/file-explorer-folder-node";

@Component({
  selector: 'app-session',
  templateUrl: './session.component.html',
  styleUrls: ['./session.component.scss']
})
export class SessionComponent implements OnInit, OnDestroy {
  private _unsubscribe$ = new Subject();
  public fileName: string | undefined;
  private _sessionCurrentDirectory: FileExplorerFolderNode | undefined;

  constructor(public element: ElementRef,
              private _playbackSessionRepository: CurrentPlaybackSessionRepositoryService,
              private _nodeRepository: FileExplorerNodeRepositoryService){
  }

  public ngOnInit(): void {
    this._playbackSessionRepository.currentSession$
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(s => {
        this.fileName = s?.currentNode.name ?? '';
        this._sessionCurrentDirectory = s?.currentNode.parentFolder;
      });
  }

  public get disabled(): boolean {
    return this._sessionCurrentDirectory?.disabled ?? true;
  }

  public ngOnDestroy(): void {
    this._unsubscribe$.next(null);
    this._unsubscribe$.complete();
  }

  public onClick(): void {
    this._nodeRepository.changeDirectory(this._sessionCurrentDirectory);
  }
}
