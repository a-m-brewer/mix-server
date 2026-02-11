import {FileExplorerFileNode} from "../../../main-content/file-explorer/models/file-explorer-file-node";
import {PlaybackState} from "./playback-state";
import {StreamKeyDto} from "../../../generated-clients/mix-server-clients";

export interface IPlaybackSession {
  id: string;
  currentNode: FileExplorerFileNode,
  deviceId: string | null | undefined,
  streamKey: StreamKeyDto,
}

export class PlaybackSession implements IPlaybackSession {
  constructor(public id: string,
              public currentNode: FileExplorerFileNode,
              public state: PlaybackState,
              public streamKey: StreamKeyDto) {
  }

  public static copy(session: IPlaybackSession,
                     state: PlaybackState) : PlaybackSession {
    return new PlaybackSession(
      session.id,
      session.currentNode,
      state,
      new StreamKeyDto({
        key: session.streamKey.key,
        expires: session.streamKey.expires
      }));
  }

  public get deviceId(): string | null | undefined {
    return this.state.deviceId;
  }

  public set deviceId(value: string | null | undefined) {
    this.state.deviceId = value;
  }
}
