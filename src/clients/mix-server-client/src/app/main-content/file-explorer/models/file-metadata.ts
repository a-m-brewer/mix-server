import {TranscodeState} from "../../../generated-clients/mix-server-clients";
import {MediaInfo} from "./media-info";

export class FileMetadata {
  constructor(public mimeType: string,
              public isMedia: boolean,
              public mediaInfo: MediaInfo | null | undefined,
              public transcodeState: TranscodeState) {
  }

  copy() {
    return new FileMetadata(this.mimeType, this.isMedia, this.mediaInfo?.copy(), this.transcodeState);
  }
}
