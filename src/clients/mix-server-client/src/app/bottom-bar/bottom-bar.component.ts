import {Component, HostListener, OnDestroy, OnInit} from '@angular/core';
import {Subject, takeUntil} from "rxjs";
import {WindowSizeRepositoryService} from "../services/repositories/window-size-repository.service";
import {WindowType} from "../services/repositories/enums/window-type";

@Component({
  selector: 'app-bottom-bar',
  templateUrl: './bottom-bar.component.html',
  styleUrls: ['./bottom-bar.component.scss']
})
export class BottomBarComponent implements OnInit, OnDestroy {
  protected readonly WindowType = WindowType;

  private _unsubscribe$ = new Subject();

  public windowType: WindowType = WindowType.Unknown;

  constructor(private _windowSizeRepository: WindowSizeRepositoryService) {
  }

  public ngOnInit(): void {
    this._windowSizeRepository.windowType$
      .pipe(
        takeUntil(this._unsubscribe$)
      )
      .subscribe(windowType => {
        this.windowType = windowType;
      });
  }

  public ngOnDestroy(): void {
    this._unsubscribe$.next(null);
    this._unsubscribe$.complete();
  }
}
