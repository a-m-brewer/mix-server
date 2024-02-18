import {Injectable} from '@angular/core';
import {
  FileExplorerNodeStateClass,
  FileExplorerNodeStateInterface
} from "../../main-content/file-explorer/models/file-explorer-node-state";
import {AudioPlayerStateService} from "../audio-player/audio-player-state.service";
import {QueueEditFormRepositoryService} from "./queue-edit-form-repository.service";
import {FileExplorerPlayingState} from "../../main-content/file-explorer/enums/file-explorer-playing-state";
import {BehaviorSubject, combineLatestWith, map} from "rxjs";

@Injectable({
  providedIn: 'root'
})
export class FileExplorerNodeStateRepositoryService {
  private _inUseFileExplorerPaths = new BehaviorSubject<string[]>([]);
  private _inUseQueuePaths = new BehaviorSubject<string[]>([]);
  private _inUseHistoryPaths = new BehaviorSubject<string[]>([]);

  private _states: { [absolutePath: string]: FileExplorerNodeStateClass } = {};

  constructor(audioPlayerState: AudioPlayerStateService,
              queueEditFormRepository: QueueEditFormRepositoryService) {
    audioPlayerState.state$
      .subscribe((audioPlayerState) => {
        for (const key in this._states) {
          let state = this._states[key];

          state.playing = audioPlayerState?.node?.absolutePath === state.absolutePath
            ? audioPlayerState.playing
              ? FileExplorerPlayingState.Playing
              : FileExplorerPlayingState.Paused
            : FileExplorerPlayingState.None;
        }
      });

    queueEditFormRepository.editForm$
      .subscribe((editForm) => {
        for (const key in this._states) {
          let state = this._states[key];

          state.editing = editForm.editing
        }
      });

    this._inUseFileExplorerPaths
      .pipe(combineLatestWith(this._inUseQueuePaths, this._inUseHistoryPaths))
      .pipe(map(([fileExplorerPaths, queuePaths, historyPaths]) =>
        [...new Set([...fileExplorerPaths, ...queuePaths, ...historyPaths])]))
      .subscribe(inUsePaths => this.handleInUsePathsChange(inUsePaths));
  }

  public getState(absolutePath: string): FileExplorerNodeStateInterface {
    if (!this._states[absolutePath]) {
      this._states[absolutePath] = new FileExplorerNodeStateClass(absolutePath);
    }

    return this._states[absolutePath];
  }

  public setInUseFileExplorerPaths(paths: string[]) {
    this._inUseFileExplorerPaths.next(paths);
  }

  public setInUseQueuePaths(paths: string[]) {
    this._inUseQueuePaths.next(paths);
  }

  public setInUseHistoryPaths(paths: string[]) {
    this._inUseHistoryPaths.next(paths);
  }

  private clearState(absolutePath: string) {
    delete this._states[absolutePath];
  }

  private handleInUsePathsChange(inUsePaths: string[]) {
    for (const key in this._states) {
      if (!inUsePaths.includes(key)) {
        this.clearState(key);
      }
    }
  }
}
