import {Directive, ElementRef, inject} from '@angular/core';

@Directive({
  selector: '[appNodeListItemSubtitle]',
  standalone: true,
  host: {
    'class': 'node-list-item-content-text-subtitle'
  }
})
export class NodeListItemSubtitleDirective {
  _elementRef = inject<ElementRef<HTMLElement>>(ElementRef);

  constructor(...args: unknown[]);
  constructor() {}
}
