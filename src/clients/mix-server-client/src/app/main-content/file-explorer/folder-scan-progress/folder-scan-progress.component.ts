import { Component } from '@angular/core';
import {MatProgressSpinner} from "@angular/material/progress-spinner";

@Component({
  selector: 'app-folder-scan-progress',
  standalone: true,
  imports: [
    MatProgressSpinner
  ],
  templateUrl: './folder-scan-progress.component.html',
  styleUrl: './folder-scan-progress.component.scss'
})
export class FolderScanProgressComponent {

}
