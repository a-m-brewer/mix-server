import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import {HomeComponent} from "./home/home.component";
import {LoginUserComponent} from "./authentication/login-user/login-user.component";
import {HistoryPageComponent} from "./history-page/history-page.component";
import {PageRoutes} from "./page-routes.enum";
import {canActivate} from "./services/auth/auth-guard";
import {QueuePageComponent} from "./queue-page/queue-page.component";
import {AdminPageComponent} from "./admin-page/admin-page.component";
import {ResetPasswordComponent} from "./authentication/reset-password/reset-password.component";

const routes: Routes = [
  {path: PageRoutes.Files, component: HomeComponent, canActivate: [canActivate]},
  {path: PageRoutes.History, component: HistoryPageComponent, canActivate: [canActivate]},
  {path: PageRoutes.Queue, component: QueuePageComponent, canActivate: [canActivate]},
  {path: PageRoutes.Admin, component: AdminPageComponent, canActivate: [canActivate]},
  {path: PageRoutes.ResetPassword, component: ResetPasswordComponent },
  {path: PageRoutes.Login, component: LoginUserComponent},
  {path: '**', redirectTo: 'files'}
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
