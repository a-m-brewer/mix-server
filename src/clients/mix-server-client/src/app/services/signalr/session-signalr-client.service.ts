import {Injectable} from '@angular/core';
import {ISignalrClient} from "./signalr-client.interface";
import {HubConnection, HubConnectionState} from "@microsoft/signalr";
import {
  CurrentSessionUpdatedEventDto, PlaybackGrantedDto,
  PlaybackStateDto,
  SignalRUpdatePlaybackStateCommand
} from "../../generated-clients/mix-server-clients";
import {Observable, Subject} from "rxjs";
import {PlaybackSession} from "../repositories/models/playback-session";
import {PlaybackSessionConverterService} from "../converters/playback-session-converter.service";
import {PlaybackState} from "../repositories/models/playback-state";
import {SignalrClientBase} from "./signalr-client-base";
import {PlaybackGranted} from "../repositories/models/playback-granted";

@Injectable({
  providedIn: 'root'
})
export class SessionSignalrClientService extends SignalrClientBase implements ISignalrClient {
  private _currentSession$ = new Subject<PlaybackSession | null>();
  private _playbackState$ = new Subject<PlaybackState>();
  private _playbackGranted$ = new Subject<PlaybackGranted>();
  private _pauseRequested$ = new Subject<boolean>();

  constructor(private _playbackSessionConverter: PlaybackSessionConverterService) {
    super();
  }

  public currentPlaybackSessionUpdated$(): Observable<PlaybackSession | null> {
    return this._currentSession$.asObservable();
  }

  public get playbackState$(): Observable<PlaybackState> {
    return this._playbackState$.asObservable();
  }

  public get playbackGranted$(): Observable<PlaybackGranted> {
    return this._playbackGranted$.asObservable();
  }

  public get pauseRequested$(): Observable<boolean> {
    return this._pauseRequested$.asObservable();
  }

  public registerMethods(connection: HubConnection): void {
    this.connection = connection;
    connection.on(
      'CurrentSessionUpdated',
      (currentSessionResponse: object) => this.handleCurrentSessionUpdated(CurrentSessionUpdatedEventDto.fromJS(currentSessionResponse)));

    connection.on(
      'PlaybackStateUpdated',
      (obj: object) => this.handlePlaybackStateUpdated(PlaybackStateDto.fromJS(obj))
    );

    connection.on(
      'PauseRequested',
      () => this.handlePauseRequested()
    )

    connection.on(
      'PlaybackGranted',
      (obj: object) => this.handlePlaybackGranted(PlaybackGrantedDto.fromJS(obj))
    )
  }

  private handleCurrentSessionUpdated(playbackSessionDto: CurrentSessionUpdatedEventDto): void {
    const converted = playbackSessionDto.currentPlaybackSession
      ? this._playbackSessionConverter.fromDto(playbackSessionDto.currentPlaybackSession)
      : null;

    this._currentSession$.next(converted);
  }

  private handlePlaybackStateUpdated(dto: PlaybackStateDto): void {
    this._playbackState$.next(new PlaybackState(dto.currentTime, dto.deviceId, dto.playing));
  }

  private handlePauseRequested(): void {
    this._pauseRequested$.next(true);
  }

  private handlePlaybackGranted(dto: PlaybackGrantedDto): void {
    this._playbackGranted$.next(new PlaybackGranted(dto.currentTime, dto.deviceId, dto.playing, dto.useDeviceCurrentTime));
  }

  public updatePlaybackState(currentTime: number): void {
    this.send("UpdatePlaybackState", new SignalRUpdatePlaybackStateCommand({
      currentTime
    }).toJSON())
  }
}
