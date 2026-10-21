import {Component, Input, ChangeDetectionStrategy} from '@angular/core';
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
    changeDetection: ChangeDetectionStrategy.Eager,
    styleUrl: './copy-text.component.scss'
})
export class CopyTextComponent {
  @Input() public text: string = '';
}
