import {FileExplorerNode} from "./file-explorer-node";
import {FileExplorerNodeType} from "../enums/file-explorer-node-type";
import {FileExplorerFolderNode} from "./file-explorer-folder-node";
import {FileMetadata} from "./file-metadata";
import {Device} from "../../../services/repositories/models/device";
import {TranscodeState} from "../../../generated-clients/mix-server-clients";
import { NodePath } from "./node-path";

export interface FileExplorerFileNodeNameParts {
  nameWithoutExtension: string;
  nameWithoutSuffix: string;
  copyNumber?: number;
  extension?: string;
}

export class FileExplorerFileNode implements FileExplorerNode {
  private readonly _fileInvalid: boolean;
  private _requestedPlaybackDevicePlaybackSupported: boolean;

  constructor(public path: NodePath,
              public type: FileExplorerNodeType,
              public exists: boolean,
              public creationTimeUtc: Date,
              public metadata: FileMetadata,
              public serverPlaybackSupported: boolean,
              public clientPlaybackSupported: boolean,
              public parent: FileExplorerFolderNode) {
    this._fileInvalid = path.absolutePath.trim() === '' || !exists;

    this.hasTranscode = metadata.isMedia && metadata.transcodeState !== TranscodeState.None;
    this.hasCompletedTranscode = metadata.isMedia && metadata.transcodeState === TranscodeState.Completed;

    this._requestedPlaybackDevicePlaybackSupported = clientPlaybackSupported || this.hasCompletedTranscode;

    this.playbackSupported = serverPlaybackSupported && this._requestedPlaybackDevicePlaybackSupported;
    this.disabled = this._fileInvalid || !this.playbackSupported;
  }

  public disabled: boolean;
  public playbackSupported: boolean;
  public readonly hasTranscode: boolean;
  public readonly hasCompletedTranscode: boolean;

  public mdIcon: string = 'description';

  public get nameSections(): FileExplorerFileNodeNameParts | null {
    const re = /^((.+?)( - Copy)?( \(([0-9]+)\))?)(\.(.*))?$/;
    const match = re.exec(this.path.fileName);

    if (!match) {
      return null
    }

    let copyNumber = undefined;
    // is a copy (Has - Copy)
    if (match[3]) {
      if (match[5]) {
        copyNumber = parseInt(match[5]);
      } else {
        copyNumber = 1;
      }
    }

    return {
      copyNumber,
      nameWithoutExtension: match[1],
      nameWithoutSuffix: match[2],
      extension: match[7]
    };
  }

  public isEqual(node: FileExplorerNode | null | undefined): boolean {
    if (!node) {
      return false;
    }

    if (!(node instanceof FileExplorerFileNode)) {
      return false;
    }

    return this.path.isEqual(node.path);
  }

  public get playbackDisabled(): boolean {
    return !this.playbackSupported ||
      !this.exists
  }

  public get serverPlaybackDisabled(): boolean {
    return !this.serverPlaybackSupported ||
      !this.exists
  }

  public copy(): FileExplorerFileNode {
    return new FileExplorerFileNode(
      this.path.copy(),
      this.type,
      this.exists,
      this.creationTimeUtc,
      this.metadata.copy(),
      this.serverPlaybackSupported,
      this.clientPlaybackSupported,
      this.parent.copy()
    );
  }

  updateCanPlay(device: Device | null | undefined) {
    const directPlaybackSupported = device?.canPlay(this) ?? false;
    this._requestedPlaybackDevicePlaybackSupported = directPlaybackSupported || this.hasCompletedTranscode;
    this.playbackSupported = this.serverPlaybackSupported && this._requestedPlaybackDevicePlaybackSupported;
    this.disabled = this._fileInvalid || !this.playbackSupported;
  }
}
