import {AfterViewInit, Component, ElementRef, HostListener, OnDestroy, OnInit, ViewChild} from '@angular/core';
import {BehaviorSubject, combineLatest, filter, fromEvent, map, merge, Subject, takeUntil} from "rxjs";
import {
  MixServerSignalrConnectionServiceService
} from "./services/signalr/mix-server-signalr-connection-service.service";
import {resizeObservable} from "./utils/rxjs-helpers";
import {AuthenticationService} from "./services/auth/authentication.service";
import {InitializationRepositoryService} from "./services/repositories/initialization-repository.service";
import {ScrollContainerRepositoryService} from "./services/repositories/scroll-container-repository.service";
import {FileExplorerNodeRepositoryService} from "./services/repositories/file-explorer-node-repository.service";
import {FileExplorerFolderNode} from "./main-content/file-explorer/models/file-explorer-folder-node";
import {
  CurrentPlaybackSessionRepositoryService
} from "./services/repositories/current-playback-session-repository.service";
import {NavigationEnd, Router} from "@angular/router";
import {PageRoutes} from "./page-routes.enum";
import {DeviceRepositoryService} from "./services/repositories/device-repository.service";
import {ServerConnectionState} from "./services/auth/enums/ServerConnectionState";
import {VisibilityRepositoryService} from "./services/repositories/visibility-repository.service";
import {TitleService} from "./services/title/title.service";
import {ToastService} from "./services/toasts/toast-service";

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent implements OnInit, AfterViewInit, OnDestroy {
  private _unsubscribe$ = new Subject();

  public viewInitialized$ = new BehaviorSubject<boolean>(false);

  public initialized: boolean = false;

  public unauthorized: boolean = false;
  public connected: boolean = false;
  public disconnected: boolean = true;
  public disconnectedReason: string | null = 'Loading';

  public currentFolder?: FileExplorerFolderNode | null;
  public showFileExplorerToolbar: boolean = false;
  public showQueueToolbar: boolean = false;
  public nodeRepositoryLoading: boolean = false;
  public playbackSessionLoading: boolean = false;

  @ViewChild('navBar')
  public navBar?: ElementRef;

  @ViewChild('mainContent')
  public mainContent?: ElementRef;

  @ViewChild('scrollContainer')
  public scrollContainer?: ElementRef;

  @ViewChild('bottomBar')
  public bottomBar?: ElementRef;

  constructor(private _authenticationService: AuthenticationService,
              private _deviceRepository: DeviceRepositoryService,
              private _initializationRepository: InitializationRepositoryService,
              private _playbackSessionRepository: CurrentPlaybackSessionRepositoryService,
              private _nodeRepository: FileExplorerNodeRepositoryService,
              private _router: Router,
              private _scrollContainerRepository: ScrollContainerRepositoryService,
              private _signalRConnectionService: MixServerSignalrConnectionServiceService,
              private _titleService: TitleService,
              private _toastService: ToastService,
              private _visibilityRepository: VisibilityRepositoryService) {
  }

  public ngOnInit(): void {
    this._titleService.initialize();

    combineLatest([
      this._initializationRepository.initialized$,
      this.viewInitialized$
    ])
      .subscribe(value => {
        if (value.every(e => e)) {
          this.calculateMainContentHeight();

          const elements = this.getElements();

          merge(
            fromEvent(window, 'resize'),
            ...elements.otherElements.map(m => resizeObservable(m))
          )
            .pipe(takeUntil(this._unsubscribe$))
            .subscribe(() => {
              this.calculateMainContentHeight();
            });
        }
      });

    this._initializationRepository.initialized$
      .subscribe(value => {
        this.initialized = value;
      });

    this._authenticationService.initialize()
      .then()
      .catch(err => this._toastService.logServerError(err, 'failed to perform token during initialization'))
      .finally(() => this._initializationRepository.initialized = true);

    this._authenticationService.serverConnectionStatus$
      .subscribe(state => {
        this.connected = state === ServerConnectionState.Connected;
        this.unauthorized = state === ServerConnectionState.Unauthorized;
        this.disconnected = state === ServerConnectionState.Disconnected;
        this.disconnectedReason = this.disconnected ? 'Disconnected': null;
      })

    this._authenticationService.connected$
      .subscribe(connected => {
        if (connected) {
          this._signalRConnectionService.connect().then();
        }
        else {
          this._signalRConnectionService.disconnect().then();
        }
      })

    this._nodeRepository.currentFolder$
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(currentFolder => {
        this.currentFolder = currentFolder;
      });

    this._nodeRepository.loading$
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(loading => {
        this.nodeRepositoryLoading = loading;
      });

    this._playbackSessionRepository.loading$
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(loading => {
        this.playbackSessionLoading = loading;
      });

    this._router.events
      .pipe(filter((e): e is NavigationEnd => e instanceof NavigationEnd))
      .pipe(map(() => {
        const url = window.location.pathname;
        return url.startsWith('/') ? url.substring(1) : url;
      }))
      .subscribe(pathname => {
        this.showFileExplorerToolbar = PageRoutes.Files.toString() === pathname;
        this.showQueueToolbar = PageRoutes.Queue.toString() === pathname;
      })
  }

  public ngAfterViewInit(): void {
    this.viewInitialized$.next(true);
  }

  public ngOnDestroy(): void {
    this._unsubscribe$.next(null);
    this._unsubscribe$.complete();
  }

  @HostListener('document:click')
  @HostListener('document:touchstart')
  public onDocumentInteraction(): void {
    this._deviceRepository.setUserInteractedWithPage();
  }

  @HostListener('document:visibilitychange')
  public onVisibilityChanged(): void {
    this._visibilityRepository.visibility = document.visibilityState;
    if (document.visibilityState === "visible") {
      this._authenticationService.performTokenRefreshAndScheduleRefresh()
        .then()
        .catch(err => this._toastService.logServerError(err, 'failed to perform token refresh based on visibility change'));
    }
  }

  public get folderBackButtonDisabled(): boolean {
    return this.nodeRepositoryLoading ||
      this.playbackSessionLoading ||
      (this.currentFolder?.parentDirectory?.disabled ?? true) ||
      this.currentFolder?.absolutePath === '';
  }

  public logout(): void {
    this._authenticationService.logout();
  }

  public onScroll(): void {
    this._scrollContainerRepository.onScrollTop(this.scrollContainer?.nativeElement.scrollTop);
  }

  public onFolderBackButtonClicked(): void {
    const parent = this.currentFolder?.parentDirectory;
    if (!parent) {
      return;
    }

    this._nodeRepository.changeDirectory(parent);
  }

  private calculateMainContentHeight(): void {
    const elements = this.getElements();

    let height = 0;
    elements.otherElements.forEach(e => {
      height += e.offsetHeight;
    })

    if (!elements.content) {
      return;
    }

    elements.content.style.height = `calc(100vh - ${height}px`;
  }

  private toHtmlElement(ref?: ElementRef): HTMLElement {
    return ref?.nativeElement as HTMLElement;
  }

  private getElements() {
    return {
      content: this.toHtmlElement(this.mainContent),
      otherElements: [
        this.toHtmlElement(this.navBar),
        this.toHtmlElement(this.bottomBar)
      ].filter(f => f)
    }
  }
}
