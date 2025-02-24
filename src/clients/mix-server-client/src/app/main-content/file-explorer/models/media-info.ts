﻿import {FormGroup} from "@angular/forms";
import {TracklistForm} from "../../../services/tracklist/models/tracklist-form.interface";

export class MediaInfo {
  constructor(public duration: string,
              public bitrate: number,
              public tracklist: FormGroup<TracklistForm>) {
  }

  public copy() {
    return new MediaInfo(this.duration, this.bitrate, this.tracklist);
  }
}
