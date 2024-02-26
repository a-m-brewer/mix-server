import {NgModule} from '@angular/core';
import {BrowserModule} from '@angular/platform-browser';

import {AppRoutingModule} from './app-routing.module';
import {AppComponent} from './app.component';
import {MIXSERVER_BASE_URL} from './generated-clients/mix-server-clients';
import {HTTP_INTERCEPTORS, HttpClientModule} from '@angular/common/http';
import {BottomBarComponent} from './bottom-bar/bottom-bar.component';
import {AudioControlComponent} from './bottom-bar/audio-control/audio-control.component';
import {BrowserAnimationsModule} from '@angular/platform-browser/animations';
import {MatProgressBarModule} from "@angular/material/progress-bar";
import {MatIconModule} from "@angular/material/icon";
import {MatButtonModule} from "@angular/material/button";
import {MainContentComponent} from './main-content/main-content.component';
import {CommonModule} from "@angular/common";
import {FileExplorerComponent} from './main-content/file-explorer/file-explorer.component';
import {MatListModule} from "@angular/material/list";
import {MatToolbarModule} from "@angular/material/toolbar";
import {MatSliderModule} from "@angular/material/slider";
import {FormsModule, ReactiveFormsModule} from "@angular/forms";
import { HomeComponent } from './home/home.component';
import {MatFormFieldModule} from "@angular/material/form-field";
import {MatInputModule} from "@angular/material/input";
import {MatCardModule} from "@angular/material/card";
import { LoginUserComponent } from './authentication/login-user/login-user.component';
import {MatProgressSpinnerModule} from "@angular/material/progress-spinner";
import { LoadingBarComponent } from './bottom-bar/loading-bar/loading-bar.component';
import { DurationDisplayComponent } from './bottom-bar/audio-control/duration-display/duration-display.component';
import { SessionComponent } from './bottom-bar/audio-control/session/session.component';
import {getMixServerApiUrl} from "./api-url-getter";
import { HistoryPageComponent } from './history-page/history-page.component';
import {InfiniteScrollModule} from "ngx-infinite-scroll";
import {MatTooltipModule} from "@angular/material/tooltip";
import {ToastrModule} from "ngx-toastr";
import {AuthInterceptor} from "./services/auth/auth.interceptor";
import { QueuePageComponent } from './queue-page/queue-page.component';
import { FolderSortFormComponent } from './main-content/file-explorer/folder-sort-form/folder-sort-form.component';
import {MatButtonToggleModule} from "@angular/material/button-toggle";
import { AdminPageComponent } from './admin-page/admin-page.component';
import { DeviceAdminComponent } from './admin-page/device-admin/device-admin.component';
import {MatTabsModule} from "@angular/material/tabs";
import { DeleteDialogComponent } from './components/dialogs/delete-dialog/delete-dialog.component';
import {MatDialogModule} from "@angular/material/dialog";
import { NodeListItemIcon } from './components/nodes/node-list/node-list-item/node-list-item-icon/node-list-item-icon.component';
import {MatMenuModule} from "@angular/material/menu";
import { AudioContextMenuComponent } from './bottom-bar/audio-control/audio-context-menu/audio-context-menu.component';
import { NodeListItemComponent } from './components/nodes/node-list/node-list-item/node-list-item.component';
import {MatRippleModule} from "@angular/material/core";
import { OpenLocationButtonComponent } from './components/nodes/node-list/node-list-item/context-menu/open-location-button/open-location-button.component';
import { AddToQueueButtonComponent } from './components/nodes/node-list/node-list-item/context-menu/add-to-queue-button/add-to-queue-button.component';
import { RemoveFromQueueButtonComponent } from './components/nodes/node-list/node-list-item/context-menu/remove-from-queue-button/remove-from-queue-button.component';
import { QueueEditFormComponent } from './queue-page/queue-edit-form/queue-edit-form.component';
import {MatCheckboxModule} from "@angular/material/checkbox";
import { SpinnerOverlayComponent } from './components/spinner-overlay/spinner-overlay.component';
import {UserAdminComponent} from "./admin-page/user-admin/user-admin.component";
import {MatChipsModule} from "@angular/material/chips";
import {NavBarComponent} from "./nav-bar/nav-bar.component";
import {MatExpansionModule} from "@angular/material/expansion";
import {AudioControlMobileComponent} from "./bottom-bar/audio-control-mobile/audio-control-mobile.component";
import {
  AudioProgressSliderComponent
} from "./bottom-bar/audio-control/audio-progress-slider/audio-progress-slider.component";
import {
  AudioControlButtonsComponent
} from "./bottom-bar/audio-control/audio-control-buttons/audio-control-buttons.component";

@NgModule({
    declarations: [
        AppComponent,
        BottomBarComponent,
        AudioControlComponent,
        MainContentComponent,
        FileExplorerComponent,
        HomeComponent,
        LoginUserComponent,
        LoadingBarComponent,
        HistoryPageComponent,
        QueuePageComponent,
        AdminPageComponent,
        DeviceAdminComponent,
        DeleteDialogComponent,
        NodeListItemIcon,
        NodeListItemComponent,
        OpenLocationButtonComponent,
        AddToQueueButtonComponent,
        RemoveFromQueueButtonComponent,
        SpinnerOverlayComponent,
        UserAdminComponent
    ],
  imports: [
    BrowserModule,
    CommonModule,
    AppRoutingModule,
    HttpClientModule,
    BrowserAnimationsModule,
    MatProgressBarModule,
    MatIconModule,
    MatButtonModule,
    MatListModule,
    MatToolbarModule,
    MatSliderModule,
    FormsModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatCardModule,
    MatProgressSpinnerModule,
    InfiniteScrollModule,
    MatTooltipModule,
    ToastrModule.forRoot(),
    MatButtonToggleModule,
    MatTabsModule,
    MatDialogModule,
    MatMenuModule,
    MatRippleModule,
    MatCheckboxModule,
    MatChipsModule,
    NavBarComponent,
    MatExpansionModule,
    AudioControlMobileComponent,
    SessionComponent,
    AudioProgressSliderComponent,
    AudioControlButtonsComponent
  ],
    providers: [
        {
            provide: MIXSERVER_BASE_URL,
            useFactory: getMixServerApiUrl
        },
        {
            provide: HTTP_INTERCEPTORS,
            useClass: AuthInterceptor,
            multi: true
        }
    ],
    exports: [
        NodeListItemIcon
    ],
    bootstrap: [AppComponent]
})
export class AppModule {
}
