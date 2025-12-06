import {Component, OnDestroy, OnInit} from '@angular/core';
import {MatListModule} from "@angular/material/list";
import {UserRepositoryService} from "../../services/repositories/user-repository.service";
import {Subject, take, takeUntil} from "rxjs";
import {User} from "../../services/repositories/models/user";

import {RoleRepositoryService} from "../../services/repositories/role-repository.service";
import {DeleteDialogComponent} from "../../components/dialogs/delete-dialog/delete-dialog.component";
import {MatDialog} from "@angular/material/dialog";
import {Role} from "../../generated-clients/mix-server-clients";
import {UserDialogComponent, AddUserDialogResponse} from "./user-dialog/user-dialog.component";
import {ToastService} from "../../services/toasts/toast-service";
import {UserService} from "../../services/admin/user.service";
import {TemporaryPasswordDialogComponent} from "./temporary-password-dialog/temporary-password-dialog.component";
import {AuthenticationService} from "../../services/auth/authentication.service";

@Component({
    selector: 'app-user-admin',
    templateUrl: './user-admin.component.html',
    styleUrl: './user-admin.component.scss',
    standalone: false
})
export class UserAdminComponent implements OnInit, OnDestroy {
  private _unsubscribe$ = new Subject();

  public users: Array<User> = [];
  public currentUser?: User | null;

  constructor(private _authenticationService: AuthenticationService,
              private _dialog: MatDialog,
              private _roleRepository: RoleRepositoryService,
              private _toastService: ToastService,
              private _userRepository: UserRepositoryService,
              private _userService: UserService) {
  }

  public ngOnInit(): void {
    this._userRepository.users$
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe((users: Array<User>) => {
        this.users = users;
        this.currentUser = users.find(f => f.id === this._authenticationService.accessToken?.userId);
      });
  }

  public ngOnDestroy(): void {
    this._unsubscribe$.next(null);
    this._unsubscribe$.complete();
  }

  public onUserDeleted(user: User): void {
    this._dialog.open(DeleteDialogComponent, {
      data: {
        displayName: user.name
      }
    })
      .afterClosed()
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(async value => {
        if (value) {
          await this._userService.deleteUser(user.id);
        }
      });
  }

  public openCreateUserDialog(): void {
    this._dialog.open(UserDialogComponent)
      .afterClosed()
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(async value => {
        if (value instanceof AddUserDialogResponse) {
          const roles = value.isAdmin
            ? [ Role.Administrator ]
            : [];

          const temporaryPassword = await this._userService.addUser(value.username, roles);

          if (temporaryPassword) {
            this._dialog.open(TemporaryPasswordDialogComponent, {
              data: {
                temporaryPassword: temporaryPassword
              }
            });
          }

          return;
        }
      });
  }

  public onEditUser(user: User) {
    this._dialog.open(UserDialogComponent, {
      data: {
        username: user.name,
        isAdmin: user.roles.includes(Role.Administrator)
      }
    })
      .afterClosed()
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(async value => {
        if (value instanceof AddUserDialogResponse) {
          const roles = value.isAdmin
            ? [ Role.Administrator ]
            : [];

          await this._userService.updateUser(user.id, roles);
          return;
        }
      });
  }
}
