import {Injectable} from '@angular/core';
import {
  FileExplorerNodeStateClass,
  FileExplorerNodeStateInterface
} from "../../main-content/file-explorer/models/file-explorer-node-state";
import {AudioPlayerStateService} from "../audio-player/audio-player-state.service";
import {QueueEditFormRepositoryService} from "./queue-edit-form-repository.service";
import {FileExplorerPlayingState} from "../../main-content/file-explorer/enums/file-explorer-playing-state";

@Injectable({
  providedIn: 'root'
})
export class FileExplorerNodeStateRepositoryService {
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
  }

  public getState(absolutePath: string): FileExplorerNodeStateInterface {
    if (!this._states[absolutePath]) {
      this._states[absolutePath] = new FileExplorerNodeStateClass(absolutePath);
    }

    return this._states[absolutePath];
  }
}
