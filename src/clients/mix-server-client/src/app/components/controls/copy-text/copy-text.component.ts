import {Component, Input} from '@angular/core';
import {MatButtonModule} from "@angular/material/button";
import {MatInputModule} from "@angular/material/input";
import {MatIconModule} from "@angular/material/icon";

import {ClipboardModule} from "@angular/cdk/clipboard";

@Component({
    selector: 'app-copy-text',
    imports: [
    MatButtonModule,
    MatInputModule,
    MatIconModule,
    ClipboardModule
],
    templateUrl: './copy-text.component.html',
    styleUrl: './copy-text.component.scss'
})
export class CopyTextComponent {
  @Input() public text: string = '';
}
