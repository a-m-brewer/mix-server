import {FileMetadata} from "./file-metadata";
import {FormGroup} from "@angular/forms";
import {TracklistForm} from "../../../services/tracklist/models/tracklist-form.interface";

export class MediaMetadata extends FileMetadata {
  constructor(mimeType: string,
              public duration: string,
              public bitrate: number,
              public tracklist: FormGroup<TracklistForm>) {
    super(mimeType);
  }

  override copy() {
    return new MediaMetadata(this.mimeType, this.duration, this.bitrate, this.tracklist);
  }
}
