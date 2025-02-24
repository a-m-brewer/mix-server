import {NodePath} from "../../repositories/models/node-path";
import {MediaInfo} from "../../../main-content/file-explorer/models/media-info";

export interface MediaInfoUpdatedEventItem {
  nodePath: NodePath
  info: MediaInfo
}

export interface MediaInfoUpdatedEvent {
  mediaInfo: Array<MediaInfoUpdatedEventItem>;
}

export interface MediaInfoRemovedEvent {
  nodePaths: Array<NodePath>;
}
