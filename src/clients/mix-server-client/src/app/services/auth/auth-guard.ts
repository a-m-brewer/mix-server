import {ActivatedRouteSnapshot, CanActivateChildFn, CanActivateFn, Router, RouterStateSnapshot} from "@angular/router";
import {inject} from "@angular/core";
import {AuthenticationService} from "./authentication.service";
import {InitializationRepositoryService} from "../repositories/initialization-repository.service";
import {ServerConnectionState} from "./enums/ServerConnectionState";
import {PageRoutes} from "../../page-routes.enum";

export const canActivate: CanActivateFn = async (
  route: ActivatedRouteSnapshot,
  state: RouterStateSnapshot
) => {
  const authService = inject(AuthenticationService)
  const router = inject(Router);
  // const initRepo = inject(InitializationRepositoryService);

  if (authService.serverConnectionStatus === ServerConnectionState.Unauthorized) {
    await router.navigate([PageRoutes.Login]);
    return false;
  }

  if (authService.serverConnectionStatus === ServerConnectionState.AwaitingPasswordReset) {
    await router.navigate([PageRoutes.ResetPassword]);
    return false;
  }

  return true;
};


export const canActivateChild: CanActivateChildFn = (route: ActivatedRouteSnapshot, state: RouterStateSnapshot) => canActivate(route, state);
