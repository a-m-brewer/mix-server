import {MediaInfo} from "../../../main-content/file-explorer/models/media-info";
import {NodePath} from "../../../main-content/file-explorer/models/node-path";

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
