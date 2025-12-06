import {Component, Input} from '@angular/core';
import {InstanceOfPipe} from "../../../../../../pipes/instance-of.pipe";

import {FileMetadata} from "../../../../../../main-content/file-explorer/models/file-metadata";

@Component({
    selector: 'app-file-metadata-subtitle',
    imports: [],
    templateUrl: './file-metadata-subtitle.component.html',
    styleUrl: './file-metadata-subtitle.component.scss'
})
export class FileMetadataSubtitleComponent {
    @Input() metadata?: FileMetadata
}
