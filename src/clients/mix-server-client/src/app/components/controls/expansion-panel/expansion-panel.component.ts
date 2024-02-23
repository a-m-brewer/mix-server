import {Component, ElementRef, EventEmitter, Input, Output, ViewChild} from '@angular/core';
import {MatRippleModule} from "@angular/material/core";
import {MatProgressBarModule} from "@angular/material/progress-bar";
import {NgIf} from "@angular/common";

@Component({
  selector: 'app-expansion-panel',
  standalone: true,
  imports: [
    MatRippleModule,
    MatProgressBarModule,
    NgIf
  ],
  templateUrl: './expansion-panel.component.html',
  styleUrl: './expansion-panel.component.scss'
})
export class ExpansionPanelComponent {
  @Output()
  public expanded = new EventEmitter<boolean>();

  @ViewChild('expansionPanelContent')
  public expansionPanelContent?: ElementRef;

  private get expansionPanelContentElement(): HTMLElement | null {
    if (!this.expansionPanelContent || !this.expansionPanelContent.nativeElement || !(this.expansionPanelContent.nativeElement instanceof HTMLElement)) {
      return null;
    }
    return this.expansionPanelContent.nativeElement;
  }

  public togglePanel(): void {
    if (!this.expansionPanelContentElement) {
      return;
    }

    if (!this.expansionPanelContentElement.style.maxHeight ||
         this.expansionPanelContentElement.style.maxHeight === '0' ||
         this.expansionPanelContentElement.style.maxHeight === '0px') {
      this.expansionPanelContentElement.style.maxHeight = this.expansionPanelContentElement.scrollHeight + "px";
      this.expanded.emit(true);
    } else {
      this.expansionPanelContentElement.style.maxHeight = '0';
      this.expanded.emit(false);
    }
  }
}
