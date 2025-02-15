import {Component, forwardRef, Input} from '@angular/core';
import {ContextMenuButton} from "../context-menu-button";
import {FileExplorerFileNode} from "../../../../../../main-content/file-explorer/models/file-explorer-file-node";
import {MatIcon} from "@angular/material/icon";
import {MatMenuItem} from "@angular/material/menu";
import {NgIf} from "@angular/common";

@Component({
  selector: 'app-request-transcode-button',
  standalone: true,
  imports: [
    MatIcon,
    MatMenuItem,
    NgIf
  ],
  templateUrl: './request-transcode-button.component.html',
  styleUrl: './request-transcode-button.component.scss',
  providers: [{provide: ContextMenuButton, useExisting: forwardRef(() => RequestTranscodeButtonComponent)}]
})
export class RequestTranscodeButtonComponent extends ContextMenuButton {
  public disabled = false;

  constructor() {
    super();
  }

  public fileVar: FileExplorerFileNode | undefined | null;

  @Input()
  public set file(value: FileExplorerFileNode | undefined | null) {
    this.fileVar = value;
    this.disabled = !this.fileVar || this.fileVar.hasTranscode;
  };

  requestTranscode() {
    if (this.disabled) {
      return;
    }
  }
}
