import {Component, Input} from '@angular/core';
import {InstanceOfPipe} from "../../../../../../pipes/instance-of.pipe";
import {NgIf} from "@angular/common";
import {MediaMetadata} from "../../../../../../main-content/file-explorer/models/media-metadata";
import {FileMetadata} from "../../../../../../main-content/file-explorer/models/file-metadata";

@Component({
  selector: 'app-file-metadata-subtitle',
  standalone: true,
    imports: [
        InstanceOfPipe,
        NgIf
    ],
  templateUrl: './file-metadata-subtitle.component.html',
  styleUrl: './file-metadata-subtitle.component.scss'
})
export class FileMetadataSubtitleComponent {
    protected readonly MediaMetadata = MediaMetadata;

    @Input() metadata?: FileMetadata
}
