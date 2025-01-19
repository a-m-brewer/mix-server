import {Directive, ElementRef, inject} from '@angular/core';

@Directive({
  selector: '[appNodeListItemTitle]',
  standalone: true,
  host: {
    'class': 'node-list-item-content-text-title'
  }
})
export class NodeListItemTitleDirective {
  _elementRef = inject<ElementRef<HTMLElement>>(ElementRef);

  constructor(...args: unknown[]);
  constructor() {}
}
