import {Role} from "../../../generated-clients/mix-server-clients";


export class User {
  public isOwner: boolean;
  public isAdmin: boolean;

  constructor(public id: string,
              public name: string,
              public roles: Role[]) {
    this.isOwner = this.inRole(Role.Owner);
    this.isAdmin = this.inRole(Role.Administrator);
  }

  public inRole(role: Role) {
    return this.roles.includes(role);
  }
}
