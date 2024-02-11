import {Injectable} from '@angular/core';
import {UserClient} from "../../generated-clients/mix-server-clients";
import {firstValueFrom} from "rxjs";
import {AddUserCommand, Role, UpdateUserCommand} from "../../generated-clients/mix-server-clients";
import {ToastService} from "../toasts/toast-service";

@Injectable({
  providedIn: 'root'
})
export class UserService {

  constructor(private _toastService: ToastService,
              private _userClient: UserClient) {
  }

  public async addUser(username: string, roles: Role[]): Promise<string | null> {
    const result = await firstValueFrom(this._userClient.addUser(new AddUserCommand({
      username,
      roles
    })))
      .catch(err => {
        this._toastService.logServerError(err, 'Failed to add user');
        return null;
      });

    if (!result) {
      return null;
    }

    return result.temporaryPassword;
  }

  public async deleteUser(userId: string): Promise<void> {
    await firstValueFrom(this._userClient.deleteUser(userId))
      .catch(err => {
        this._toastService.logServerError(err, 'Failed to delete user');
      });
  }

  public async updateUser(id: string, roles: Role[]): Promise<void> {
    await firstValueFrom(this._userClient.updateUser(id, new UpdateUserCommand({ roles })))
      .catch(err => {
        this._toastService.logServerError(err, 'Failed to update user roles');
      });
  }
}
