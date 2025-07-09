import {NodePathHeader} from "../../../main-content/file-explorer/models/node-path";
import {FormGroup} from "@angular/forms";
import {TracklistForm} from "../../tracklist/models/tracklist-form.interface";

export class TracklistUpdatedEvent {
  constructor(public path: NodePathHeader,
              public tracklist: FormGroup<TracklistForm>) {
  }
}
