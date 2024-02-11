import { Injectable } from '@angular/core';
import {GetAllUsersResponse, UserDto} from "../../generated-clients/mix-server-clients";
import {User} from "../repositories/models/user";

@Injectable({
  providedIn: 'root'
})
export class UserConverterService {

  constructor() { }

  public fromGetAllResponse(response: GetAllUsersResponse): User[] {
    return response.users.map(m => this.fromDto(m));
  }

  public fromDto(dto: UserDto): User {
    return new User(dto.userId, dto.username, dto.roles);
  }
}
