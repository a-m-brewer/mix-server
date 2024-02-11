import {Observable} from "rxjs";

export function resizeObservable(element: HTMLElement): Observable<Array<ResizeObserverEntry>> {
  return new Observable<Array<ResizeObserverEntry>>(subscriber => {
    const ro = new ResizeObserver(entries => {
      subscriber.next(entries);
    });

    // Observe one or multiple elements
    ro.observe(element);
    return function unsubscribe() {
      ro.unobserve(element);
    }
  });
}
