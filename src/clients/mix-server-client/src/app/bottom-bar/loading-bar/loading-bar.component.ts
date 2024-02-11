import {Component, OnDestroy, OnInit} from '@angular/core';
import {LoadingRepositoryService} from "../../services/repositories/loading-repository.service";
import {Subject, takeUntil} from "rxjs";

@Component({
  selector: 'app-loading-bar',
  templateUrl: './loading-bar.component.html',
  styleUrls: ['./loading-bar.component.scss']
})
export class LoadingBarComponent implements OnInit, OnDestroy {
  private _unsubscribe$ = new Subject();
  public loading: boolean = false;

  constructor(private _loadingRepository: LoadingRepositoryService) {
  }

  public ngOnInit(): void {
    this._loadingRepository.loading$()
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(value => {
        this.loading = value;
      });
  }

  public ngOnDestroy(): void {
    this._unsubscribe$.next(null);
    this._unsubscribe$.complete();
  }
}
