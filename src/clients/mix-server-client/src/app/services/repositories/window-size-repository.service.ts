import {Injectable} from '@angular/core';
import {BehaviorSubject, fromEvent, Observable} from "rxjs";
import {WindowType} from "./enums/window-type";

@Injectable({
  providedIn: 'root'
})
export class WindowSizeRepositoryService {
  private _windowType$ = new BehaviorSubject<WindowType>(this.calculateWindowType());

  constructor() {
    window.matchMedia("(max-width: 600px)").addEventListener('change', (event) => {
      const nextWindowType = this.calculateWindowType();

      this._windowType$.next(nextWindowType);
    });

    window.matchMedia("(min-width: 601px)").addEventListener('change', (event) => {
      const nextWindowType = this.calculateWindowType();

      this._windowType$.next(nextWindowType);
    });
  }

  public get windowResized$(): Observable<Event> {
    return fromEvent(window, 'resize');
  }

  public get windowType$(): Observable<WindowType> {
    return this._windowType$.asObservable();
  }

  public get windowType(): WindowType {
    return this._windowType$.getValue();
  }

  private calculateWindowType(): WindowType {
    if (!window) {
      return WindowType.Unknown;
    }

    return window.innerWidth <= 600
      ? WindowType.Mobile
      : WindowType.Desktop;
  }
}
