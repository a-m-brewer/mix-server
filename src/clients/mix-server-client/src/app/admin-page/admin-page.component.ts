import {Component, OnDestroy, OnInit} from '@angular/core';
import {RoleRepositoryService} from "../services/repositories/role-repository.service";
import {Subject, takeUntil} from "rxjs";
import {Role} from "../generated-clients/mix-server-clients";

@Component({
  selector: 'app-admin-page',
  templateUrl: './admin-page.component.html',
  styleUrls: ['./admin-page.component.scss']
})
export class AdminPageComponent implements OnInit, OnDestroy {
  private _unsubscribe$ = new Subject();

  public isAdmin = false;

  constructor(private _roleRepository: RoleRepositoryService) {
  }

  public ngOnInit(): void {
    this._roleRepository.inRole$(Role.Administrator)
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(isAdmin => this.isAdmin = isAdmin);
  }

  public ngOnDestroy(): void {
    this._unsubscribe$.next(null);
    this._unsubscribe$.complete();
  }
}
