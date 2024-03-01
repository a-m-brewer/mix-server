import { Injectable } from '@angular/core';
import {FileExplorerFileNode} from "../../main-content/file-explorer/models/file-explorer-file-node";
import {firstValueFrom} from "rxjs";
import {
  CurrentSessionUpdatedDto,
  SessionClient,
  SetCurrentSessionCommand,
  SetNextSessionCommand
} from "../../generated-clients/mix-server-clients";
import {LoadingRepositoryService} from "../repositories/loading-repository.service";
import {PlaybackSessionConverterService} from "../converters/playback-session-converter.service";
import {ToastService} from "../toasts/toast-service";
import {CurrentPlaybackSessionRepositoryService} from "../repositories/current-playback-session-repository.service";
import {QueueRepositoryService} from "../repositories/queue-repository.service";
import {QueueConverterService} from "../converters/queue-converter.service";
import {AuthenticationService} from "../auth/authentication.service";

@Injectable({
  providedIn: 'root'
})
export class SessionService {

  constructor(
    private _authenticationService: AuthenticationService,
    private _loadingRepository: LoadingRepositoryService,
    private _playbackSessionConverter: PlaybackSessionConverterService,
    private _playbackSessionRepository: CurrentPlaybackSessionRepositoryService,
    private _sessionClient: SessionClient,
    private _toastService: ToastService,
    private _queueConverter: QueueConverterService,
    private _queueRepository: QueueRepositoryService) { }

  public setFile(file: FileExplorerFileNode): void  {
    this._loadingRepository.startLoadingId(file.absolutePath);

    firstValueFrom(this._sessionClient.setCurrentSession(new SetCurrentSessionCommand({
      absoluteFolderPath: file.parent.absolutePath,
      fileName: file.name
    })))
      .then(dto => this.next(dto))
      .catch(err => this._toastService.logServerError(err, 'Failed to set current session'))
      .finally(() => this._loadingRepository.stopLoadingId(file.absolutePath));
  }

  public clearSession(): void {
    this._loadingRepository.startLoading();
    firstValueFrom(this._sessionClient.clearCurrentSession())
      .then(dto => this.next(dto))
      .catch(err => this._toastService.logServerError(err, 'Failed to clear current session'))
      .finally(() => this._loadingRepository.stopLoading());
  }

  public back(): void {
    this.setNextSession(new SetNextSessionCommand({
      offset: -1,
      resetSessionState: false
    }))
  }

  public skip(): void {
    this.setNextSession(new SetNextSessionCommand({
      offset: 1,
      resetSessionState: false
    }))
  }

  public setSessionEnded(): void {
    const currentSession = this._playbackSessionRepository.currentSession;
    if (!currentSession || currentSession.state.deviceId !== this._authenticationService.deviceId) {
      return;
    }

    this.setNextSession(new SetNextSessionCommand({
      offset: 1,
      resetSessionState: true
    }));
  }

  private setNextSession(command: SetNextSessionCommand): void {
    this._loadingRepository.startLoading();
    firstValueFrom(this._sessionClient.setNextSession(command))
      .then(dto => this.next(dto))
      .catch(err => this._toastService.logServerError(err, 'Failed to set next session'))
      .finally(() => this._loadingRepository.stopLoading());
  }

  private next(dto: CurrentSessionUpdatedDto): void {
    const session = dto.session ? this._playbackSessionConverter.fromDto(dto.session) : null;
    const queue = this._queueConverter.fromDto(dto.queue);

    this._playbackSessionRepository.currentSession = session;
    this._queueRepository.queue = queue;
  }
}
