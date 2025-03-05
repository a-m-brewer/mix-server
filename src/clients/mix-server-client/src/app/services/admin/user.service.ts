import {Injectable} from '@angular/core';
import {AddUserCommand, Role, UpdateUserCommand} from "../../generated-clients/mix-server-clients";
import {UserApiService} from "../api.service";

@Injectable({
  providedIn: 'root'
})
export class UserService {

  constructor(private _userClient: UserApiService) {
  }

  public async addUser(username: string, roles: Role[]): Promise<string | null> {
    const result = await this._userClient.request('AddUser',
      client => client.addUser(new AddUserCommand({
        username,
        roles
      })), 'Failed to add user');

    return result.result?.temporaryPassword ?? null;
  }

  public async deleteUser(userId: string): Promise<void> {
    await this._userClient.request('DeleteUser', client => client.deleteUser(userId), 'Failed to delete user');
  }

  public async updateUser(id: string, roles: Role[]): Promise<void> {
    await this._userClient.request('UpdateUser', client => client.updateUser(id, new UpdateUserCommand({ roles })), 'Failed to update user');
  }
}
