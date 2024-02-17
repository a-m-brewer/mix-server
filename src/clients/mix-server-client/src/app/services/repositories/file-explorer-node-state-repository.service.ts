import {Injectable} from '@angular/core';
import {
  FileExplorerNodeStateClass,
  FileExplorerNodeStateInterface
} from "../../main-content/file-explorer/models/file-explorer-node-state";
import {AudioPlayerStateService} from "../audio-player/audio-player-state.service";
import {FileExplorerNodeState} from "../../main-content/file-explorer/enums/file-explorer-node-state.enum";

@Injectable({
  providedIn: 'root'
})
export class FileExplorerNodeStateRepositoryService {
  private _states: { [absolutePath: string]: FileExplorerNodeStateClass } = {};

  constructor(audioPlayerState: AudioPlayerStateService) {
    audioPlayerState.state$
      .subscribe((audioPlayerState) => {
        for (const key in this._states) {
          let state = this._states[key];

          if (audioPlayerState?.node?.absolutePath === state.absolutePath) {
            state.folderState = audioPlayerState.playing ? FileExplorerNodeState.Playing : FileExplorerNodeState.Paused;
          } else if (state.folderState === FileExplorerNodeState.Playing ||state.folderState === FileExplorerNodeState.Paused) {
            state.folderState = FileExplorerNodeState.None;
          }
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
