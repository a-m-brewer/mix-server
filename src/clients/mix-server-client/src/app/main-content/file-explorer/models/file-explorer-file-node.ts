import {FileExplorerNode} from "./file-explorer-node";
import {FileExplorerNodeType} from "../enums/file-explorer-node-type";
import {FileExplorerNodeState} from "../enums/file-explorer-node-state.enum";
import {FileExplorerFolderNode} from "./file-explorer-folder-node";
import {AudioPlayerState} from "../../../services/audio-player/models/audio-player-state";

export class FileExplorerFileNode extends FileExplorerNode {
  constructor(name: string,
              nameIdentifier: string,
              absolutePath: string | null | undefined,
              exists: boolean,
              public playbackSupported: boolean,
              imageUrl: string | undefined,
              public parentFolder: FileExplorerFolderNode) {
    super(name, nameIdentifier, absolutePath, FileExplorerNodeType.File, exists, 'description', imageUrl);
  }

  public get disabled(): boolean {
    return this.playbackDisabled ||
      this.isCurrentSession
  }

  public get playbackDisabled(): boolean {
    return !this.playbackSupported ||
      !this.exists
  }

  public isEqual(other?: FileExplorerNode | null): boolean {
    if (!other) {
      return false;
    }

    if (!(other instanceof FileExplorerFileNode)) {
      return false;
    }

    const otherAsType = other as FileExplorerFileNode;

    return this.name === otherAsType.name && this.parentFolder.isEqual(otherAsType.parentFolder);
  }

  public updateState(state: AudioPlayerState | null | undefined) {
    if (state?.node?.absolutePath && state.node.absolutePath === this.absolutePath) {
      this.state = state.playing ? FileExplorerNodeState.Playing : FileExplorerNodeState.Paused;
    }
    else if (this.isCurrentSession) {
      this.state = FileExplorerNodeState.None;
    }
  }

  public equal(currentNode: FileExplorerFileNode): boolean {
    return false;
  }
}
