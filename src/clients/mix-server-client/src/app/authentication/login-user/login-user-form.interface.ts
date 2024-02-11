import {FormControl} from "@angular/forms";

export interface LoginUserForm {
  username: FormControl<string>;
  password: FormControl<string>;
}
