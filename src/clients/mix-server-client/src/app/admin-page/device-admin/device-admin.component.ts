import {Component, OnDestroy, OnInit} from '@angular/core';
import {DeviceRepositoryService} from "../../services/repositories/device-repository.service";
import {Subject, takeUntil} from "rxjs";
import {Device} from "../../services/repositories/models/device";
import {MatDialog} from "@angular/material/dialog";
import {DeleteDialogComponent} from "../../components/dialogs/delete-dialog/delete-dialog.component";

@Component({
    selector: 'app-device-admin',
    templateUrl: './device-admin.component.html',
    styleUrls: ['./device-admin.component.scss'],
    standalone: false
})
export class DeviceAdminComponent implements OnInit, OnDestroy {
  private _unsubscribe$ = new Subject();

  public devices: Device[] = [];

  constructor(private _deviceRepository: DeviceRepositoryService,
              private _dialog: MatDialog) {
  }

  public ngOnInit(): void {
    this._deviceRepository.devices$
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(next => {
        this.devices = next;
      })
  }

  public ngOnDestroy(): void {
    this._unsubscribe$.next(null);
    this._unsubscribe$.complete();
  }

  public onDeviceDeleted(device: Device): void {
    this._dialog.open(DeleteDialogComponent, {
      data: device
    })
      .afterClosed()
      .subscribe((value: boolean) => {
        if (value) {
          this._deviceRepository.delete(device);
        }
      });
  }
}
