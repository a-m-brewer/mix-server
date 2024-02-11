import {Component, OnDestroy, OnInit} from '@angular/core';
import {AudioPlayerService} from "../../services/audio-player/audio-player.service";
import {Subject, takeUntil} from "rxjs";
import {CurrentPlaybackSessionRepositoryService} from "../../services/repositories/current-playback-session-repository.service";
import {IPlaybackSession} from "../../services/repositories/models/playback-session";
import {QueueRepositoryService} from "../../services/repositories/queue-repository.service";
import {FileExplorerFileNode} from "../../main-content/file-explorer/models/file-explorer-file-node";
import {DeviceRepositoryService} from "../../services/repositories/device-repository.service";
import {Device} from "../../services/repositories/models/device";
import {MatSliderDragEvent} from "@angular/material/slider";
import {AuthenticationService} from "../../services/auth/authentication.service";

@Component({
  selector: 'app-audio-control',
  templateUrl: './audio-control.component.html',
  styleUrls: ['./audio-control.component.scss']
})
export class AudioControlComponent implements OnInit, OnDestroy {
  private _unsubscribe$ = new Subject();
  private _volumeBeforeMute = 0;
  private _currentTime = 0;

  public currentPlaybackSession: IPlaybackSession | null | undefined;
  public currentDevice: Device | null | undefined;
  public currentPlaybackDevice: Device | null | undefined;

  public previousFile: FileExplorerFileNode | null | undefined;
  public nextFile: FileExplorerFileNode | null | undefined;

  public isCurrentPlaybackDevice: boolean = false;
  public currentSessionPlaying: boolean = false;
  public disconnected: boolean = true;

  constructor(private _authService: AuthenticationService,
              private _audioPlayer: AudioPlayerService,
              private _deviceRepository: DeviceRepositoryService,
              private _playbackSessionRepository: CurrentPlaybackSessionRepositoryService,
              private _queueRepository: QueueRepositoryService) {
  }

  public ngOnInit(): void {
    this._audioPlayer.initialize();
    this._audioPlayer.currentTime$
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe((v: number) => {
        this._currentTime = v;
      });

    this._playbackSessionRepository
      .currentSession$
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe((playbackSession: IPlaybackSession | null) => {
        this.currentPlaybackSession = playbackSession
        this.updateIsCurrentPlaybackDevice();
      });

    this._playbackSessionRepository
      .currentPlaybackDevice$
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(deviceId => {
        if (this.currentPlaybackSession) {
          this.currentPlaybackSession.deviceId = deviceId;
        }
        this.updateIsCurrentPlaybackDevice();
      });

    this._playbackSessionRepository
      .currentSessionPlaying$
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(playing => {
        this.currentSessionPlaying = playing;
      })

    this._deviceRepository
      .currentDevice$
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(device => {
        this.currentDevice = device;
        this.updateIsCurrentPlaybackDevice();
      });

    this._authService
      .connected$
      .subscribe(connected => this.disconnected = !connected);

    this._queueRepository
      .nextQueueItem$()
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(item => this.nextFile = item?.file);

    this._queueRepository
      .previousQueueItem$()
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(item => this.previousFile = item?.file);

    this._volumeBeforeMute = this._audioPlayer.volume;
  }

  public ngOnDestroy(): void {
    this._audioPlayer.dispose();
    this._unsubscribe$.next(null);
    this._unsubscribe$.complete();
  }

  public get playing(): boolean {
    if (this.isCurrentPlaybackDevice) {
      return this._audioPlayer.playing;
    }

    return this.currentSessionPlaying;
  }

  public get currentTime(): number {
    return this._currentTime;
  }

  public set currentTime(value: number) {
    this._audioPlayer.currentTime = value;
  }

  public get duration(): number {
    return this._audioPlayer.duration;
  }

  public get volume(): number {
    return this._audioPlayer.volume;
  }

  public set volume(value: number) {
    this._audioPlayer.volume = value;
    if (this.muted) {
      this._audioPlayer.muted = false;
    }
  }

  public get muted(): boolean {
    return this._audioPlayer.muted;
  }

  public play(): void {
    this._audioPlayer.requestPlayback(this.currentPlaybackDevice?.id).then();
  }

  public pause(): void {
    this._audioPlayer.requestPause();
  }

  public skipPrevious(): void {
    if (!this.previousFile) {
      return;
    }

    this._playbackSessionRepository.back();
  }

  public skipNext(): void {
    if (!this.nextFile) {
      return;
    }

    this._playbackSessionRepository.skip();
  }

  public backward(): void {
    this._audioPlayer.seekOffset(-30);
  }

  public forward(): void {
    this._audioPlayer.seekOffset(30);
  }

  public toggleMute(): void {
    this._audioPlayer.muted = !this._audioPlayer.muted;
    if (this._audioPlayer.muted) {
      this._volumeBeforeMute = this.volume;
      this._audioPlayer.volume = 0;
    }
    else {
      this._audioPlayer.volume = this._volumeBeforeMute;
    }
  }

  public sliderDragEnded(event: MatSliderDragEvent) {
    this._audioPlayer.seek(event.value);
  }

  private updateIsCurrentPlaybackDevice(): void {
    if (!this.currentPlaybackSession) {
      this.currentPlaybackDevice = null;
    }

    if (!this.currentDevice || !this.currentPlaybackSession) {
      this.isCurrentPlaybackDevice = false;
      return;
    }

    this.isCurrentPlaybackDevice = this.currentDevice.id === this.currentPlaybackSession.deviceId;
    this.currentPlaybackDevice = this._deviceRepository.getDevice(this.currentPlaybackSession.deviceId);
  }
}
