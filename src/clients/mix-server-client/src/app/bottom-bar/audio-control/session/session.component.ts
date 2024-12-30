import {Component, ElementRef, OnDestroy, OnInit, ViewChild} from '@angular/core';
import {Subject, takeUntil} from "rxjs";
import {CurrentPlaybackSessionRepositoryService} from "../../../services/repositories/current-playback-session-repository.service";
import {FileExplorerNodeRepositoryService} from "../../../services/repositories/file-explorer-node-repository.service";
import {FileExplorerFolderNode} from "../../../main-content/file-explorer/models/file-explorer-folder-node";
import {MatButtonModule} from "@angular/material/button";
import {Router} from "@angular/router";
import {PageRoutes} from "../../../page-routes.enum";

@Component({
  selector: 'app-session',
  standalone: true,
  imports: [
    MatButtonModule
  ],
  templateUrl: './session.component.html',
  styleUrls: ['./session.component.scss']
})
export class SessionComponent implements OnInit, OnDestroy {
  private _unsubscribe$ = new Subject();
  public fileName: string | undefined;
  public sessionCurrentDirectory: FileExplorerFolderNode | undefined;

  constructor(private _playbackSessionRepository: CurrentPlaybackSessionRepositoryService,
              private _router: Router){
  }

  public ngOnInit(): void {
    this._playbackSessionRepository.currentSession$
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(s => {
        this.fileName = s?.currentNode.name ?? '';
        this.sessionCurrentDirectory = s?.currentNode.parent;
      });
  }

  public ngOnDestroy(): void {
    this._unsubscribe$.next(null);
    this._unsubscribe$.complete();
  }

  public async onClick(): Promise<void> {
    if (!this.sessionCurrentDirectory) return;

    await this._router.navigate([PageRoutes.Tracklist]);
  }
}
