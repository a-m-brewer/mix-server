import {FileExplorerFileNode} from "../../../main-content/file-explorer/models/file-explorer-file-node";

export class AudioPlayerStateModel {
  constructor(public node?: FileExplorerFileNode | null,
              public queueItemId?: string | null | undefined,
              public playing: boolean = false) {
  }

  public get isDefault(): boolean {
    return !this.playing && !this.queueItemId;
  }

  public static copy(value: AudioPlayerStateModel): AudioPlayerStateModel {
    return new AudioPlayerStateModel(value.node, value.queueItemId, value.playing);
  }
}
