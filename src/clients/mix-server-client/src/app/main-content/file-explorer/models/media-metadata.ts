import {FileMetadata} from "./file-metadata";
import {FormGroup} from "@angular/forms";
import {TracklistForm} from "../../../services/tracklist/models/tracklist-form.interface";
import {TranscodeState} from "../../../generated-clients/mix-server-clients";

export class MediaMetadata extends FileMetadata {
  constructor(mimeType: string,
              public duration: string,
              public bitrate: number,
              public transcodeState: TranscodeState,
              public tracklist: FormGroup<TracklistForm>) {
    super(mimeType);
  }

  override copy() {
    return new MediaMetadata(this.mimeType, this.duration, this.bitrate, this.transcodeState, this.tracklist);
  }
}
