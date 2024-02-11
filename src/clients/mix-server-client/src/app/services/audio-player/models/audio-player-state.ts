import {FileExplorerFileNode} from "../../../main-content/file-explorer/models/file-explorer-file-node";

export class AudioPlayerState {
  constructor(public node?: FileExplorerFileNode | null,
              public queueItemId?: string | null | undefined,
              public playing: boolean = false) {
  }

  public get isDefault(): boolean {
    return !this.playing && !this.queueItemId;
  }

  public static copy(value: AudioPlayerState): AudioPlayerState {
    return new AudioPlayerState(value.node, value.queueItemId, value.playing);
  }
}
