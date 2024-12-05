import {Component, OnDestroy, OnInit} from '@angular/core';
import {MatToolbarModule} from "@angular/material/toolbar";
import {MatButtonModule} from "@angular/material/button";
import {MatMenuModule} from "@angular/material/menu";
import {MatIconModule} from "@angular/material/icon";
import {MatListModule} from "@angular/material/list";
import {AsyncPipe, NgClass, NgForOf, NgIf} from "@angular/common";
import {MenuItem, MenuLabel} from "./menu-item.interface";
import {PageRoutes} from "../page-routes.enum";
import {NavigationEnd, Router, RouterLink} from "@angular/router";
import {LoadingRepositoryService} from "../services/repositories/loading-repository.service";
import {LoadingNodeStatus, LoadingNodeStatusImpl} from "../services/repositories/models/loading-node-status";
import {BehaviorSubject, filter, map, Observable, Subject, takeUntil} from "rxjs";
import {MatProgressSpinnerModule} from "@angular/material/progress-spinner";
import {AuthenticationService} from "../services/auth/authentication.service";
import {ServerConnectionState} from "../services/auth/enums/ServerConnectionState";
import {FileExplorerNodeRepositoryService} from "../services/repositories/file-explorer-node-repository.service";
import {FileExplorerFolder} from "../main-content/file-explorer/models/file-explorer-folder";
import {FolderSortFormComponent} from "../main-content/file-explorer/folder-sort-form/folder-sort-form.component";
import {QueueEditFormComponent} from "../queue-page/queue-edit-form/queue-edit-form.component";
import { WindowSizeRepositoryService } from "../services/repositories/window-size-repository.service";
import {WindowType} from "../services/repositories/enums/window-type";

@Component({
  selector: 'app-nav-bar',
  standalone: true,
  imports: [
    MatToolbarModule,
    MatButtonModule,
    MatMenuModule,
    MatIconModule,
    MatListModule,
    NgForOf,
    RouterLink,
    MatProgressSpinnerModule,
    NgIf,
    AsyncPipe,
    FolderSortFormComponent,
    QueueEditFormComponent,
    NgClass
  ],
  templateUrl: './nav-bar.component.html',
  styleUrl: './nav-bar.component.scss'
})
export class NavBarComponent implements OnInit, OnDestroy {
  private _unsubscribe$ = new Subject();
  private _menuItems$ = new BehaviorSubject<Array<MenuItem>>([
    {
      label: MenuLabel.Files,
      icon: 'album',
      show: false,
      showOnMobile: false,
      showOnTablet: true,
      showOnDesktop: true,
      route: PageRoutes.Files
    },
    {
      label: MenuLabel.Queue,
      icon: 'queue_music',
      show: false,
      showOnMobile: false,
      showOnTablet: true,
      showOnDesktop: true,
      route: PageRoutes.Queue
    },
    {
      label: MenuLabel.History,
      icon: 'history',
      show: false,
      showOnMobile: false,
      showOnTablet: true,
      showOnDesktop: true,
      route: PageRoutes.History
    },
    {
      label: MenuLabel.Admin,
      icon: 'admin_panel_settings',
      show: false,
      showOnMobile: false,
      showOnTablet: true,
      showOnDesktop: true,
      route: PageRoutes.Admin
    },
    {
      label: MenuLabel.Logout,
      icon: 'logout',
      show: false,
      showOnMobile: false,
      showOnTablet: false,
      showOnDesktop: false,
      color: 'warn',
      route: () => this._authenticationService.logout()
    },
  ]);

  public loadingStatus: LoadingNodeStatus = LoadingNodeStatusImpl.new;

  public currentPage?: MenuLabel;
  public currentFolder?: FileExplorerFolder | null;
  public windowType: WindowType = WindowType.Unknown;

  constructor(private _authenticationService: AuthenticationService,
              private _loadingRepository: LoadingRepositoryService,
              private _nodeRepository: FileExplorerNodeRepositoryService,
              private _router: Router,
              private _windowSizeRepository: WindowSizeRepositoryService) {
  }

  public ngOnInit(): void {
    this._loadingRepository.status$()
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(status => {
        this.loadingStatus = status;
      });

    this._authenticationService.serverConnectionStatus$
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(status => {
        this.refreshMenuItems(item => {
          switch (item.label) {
            case MenuLabel.Queue:
            case MenuLabel.History:
            case MenuLabel.Admin:
              item.show = status === ServerConnectionState.Connected;
              break;
            case MenuLabel.Logout:
              item.show = status !== ServerConnectionState.Unauthorized;
              break;
          }
        });
      });

    this.setCurrentPage();
    this._router.events
      .pipe(takeUntil(this._unsubscribe$))
      .pipe(filter((e): e is NavigationEnd => e instanceof NavigationEnd))
      .pipe(map(() => {
        return this.pathname;
      }))
      .subscribe(() => {
        this.setCurrentPage();
      });

    this._nodeRepository.currentFolder$
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(currentFolder => {
        this.currentFolder = currentFolder;
        this.refreshMenuItems(item => {
          if (item.label === MenuLabel.Files) {
            item.show = currentFolder && currentFolder.node.belongsToRootChild;
          }
        });
      });

    this._windowSizeRepository.windowType$
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(windowType => {
        this.windowType = windowType;
      })
  }

  public get menuItems$(): Observable<Array<MenuItem>> {
    return this._menuItems$
      .pipe(map(m => m.filter(i => i.show)));
  }

  public ngOnDestroy(): void {
    this._unsubscribe$.next(null);
    this._unsubscribe$.complete();
  }

  public filesClicked(): void {
    this._nodeRepository.changeDirectory(
      this.currentPage === MenuLabel.Files
        ? null
        : this.currentFolder?.node
    );
  }

  public navigate(item: MenuItem) {
    this._loadingRepository.startLoadingId(item.label);
    if (typeof item.route === 'string') {
      this._router.navigate([item.route])
        .finally(() => this._loadingRepository.stopLoadingId(item.label));
    }
    else {
      try {
        item.route();
      } finally {
        this._loadingRepository.stopLoadingId(item.label);
      }
    }
  }


  public refreshFolder(): void {
    this._nodeRepository.refreshFolder();
  }

  private get pathname(): string {
    const pathname = window.location.pathname;
    return pathname.startsWith('/') ? pathname.substring(1) : pathname;
  }

  private refreshMenuItems(update: (item: MenuItem) => void): void {
    const nextMenuItems = [...this._menuItems$.value];

    nextMenuItems.forEach(item => update(item));

    this._menuItems$.next(nextMenuItems);
  }

  private setCurrentPage(): void {
    switch (this.pathname) {
      case PageRoutes.Files.toString():
        this.currentPage = MenuLabel.Files;
        break;
      case PageRoutes.Queue:
        this.currentPage = MenuLabel.Queue;
        break;
      case PageRoutes.History:
        this.currentPage = MenuLabel.History;
        break;
      case PageRoutes.Admin:
        this.currentPage = MenuLabel.Admin;
        break;
      default:
        this.currentPage = undefined;
    }
  }

  protected readonly MenuLabel = MenuLabel;
  protected readonly WindowType = WindowType;
}
