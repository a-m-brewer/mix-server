import {FileExplorerFileNode} from "../../../main-content/file-explorer/models/file-explorer-file-node";
import {PlaybackState} from "./playback-state";
import {ImportTracklistDto} from "../../../generated-clients/mix-server-clients";

export interface IPlaybackSession {
  id: string;
  currentNode: FileExplorerFileNode,
  lastPlayed: Date,
  deviceId: string | null | undefined,
  autoPlay: boolean
  tracklist: ImportTracklistDto
}

export class PlaybackSession implements IPlaybackSession {
  constructor(public id: string,
              public currentNode: FileExplorerFileNode,
              public lastPlayed: Date,
              public state: PlaybackState,
              public autoPlay: boolean,
              public tracklist: ImportTracklistDto) {
  }

  public static copy(session: IPlaybackSession, state: PlaybackState) : PlaybackSession {
    return new PlaybackSession(
      session.id,
      session.currentNode,
      session.lastPlayed,
      state,
      session.autoPlay,
      session.tracklist);
  }

  public get deviceId(): string | null | undefined {
    return this.state.deviceId;
  }

  public set deviceId(value: string | null | undefined) {
    this.state.deviceId = value;
  }
}
