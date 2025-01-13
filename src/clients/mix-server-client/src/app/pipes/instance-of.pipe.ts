import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'instanceOf',
  standalone: true
})
export class InstanceOfPipe implements PipeTransform {
  transform<T>(value: any, type: new (...args: any[]) => T): T | null {
    if (value instanceof type) {
      return value as T;
    }
    return null;
  }
}
