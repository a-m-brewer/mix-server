import {Injectable} from '@angular/core';
import {FileExplorerFileNode} from "../../main-content/file-explorer/models/file-explorer-file-node";
import {firstValueFrom} from "rxjs";
import {
    CurrentSessionUpdatedDto, QueueClient,
    SessionClient,
    SetCurrentSessionCommand,
    SetNextSessionCommand, SetQueuePositionCommand
} from "../../generated-clients/mix-server-clients";
import {LoadingRepositoryService} from "../repositories/loading-repository.service";
import {PlaybackSessionConverterService} from "../converters/playback-session-converter.service";
import {ToastService} from "../toasts/toast-service";
import {CurrentPlaybackSessionRepositoryService} from "../repositories/current-playback-session-repository.service";
import {QueueRepositoryService} from "../repositories/queue-repository.service";
import {QueueConverterService} from "../converters/queue-converter.service";
import {AuthenticationService} from "../auth/authentication.service";
import {ApiService} from "../api.service";

@Injectable({
    providedIn: 'root'
})
export class SessionService {

    constructor(
        private _authenticationService: AuthenticationService,
        private _loadingRepository: LoadingRepositoryService,
        private _playbackSessionConverter: PlaybackSessionConverterService,
        private _playbackSessionRepository: CurrentPlaybackSessionRepositoryService,
        private _sessionClient: ApiService<SessionClient>,
        private _queueClient: ApiService<QueueClient>,
        private _queueConverter: QueueConverterService,
        private _queueRepository: QueueRepositoryService) {
    }

    public setFile(file: FileExplorerFileNode): void {
        this._sessionClient.request(file.absolutePath,
            client => client.setCurrentSession(new SetCurrentSessionCommand({
                absoluteFolderPath: file.parent.absolutePath,
                fileName: file.name
            })), 'Failed to set current session')
            .then(dto => this.next(dto));
    }

    public setQueuePosition(queueItemId: string): void {
        this._queueClient.request(queueItemId,
            client => client.setQueuePosition(new SetQueuePositionCommand({
                queueItemId
            })), 'Failed to set queue position')
            .then(dto => this.next(dto));
    }

    public clearSession(): void {
        this._sessionClient.request('ClearSession',
            client => client.clearCurrentSession(),
            'Failed to clear current session')
            .then(dto => this.next(dto));
    }

    public back(): void {
        const currentSession = this._playbackSessionRepository.currentSession;
        if (!currentSession) {
            return;
        }

        const offset = this._queueRepository.queue.findNextValidOffset(-1);
        if (!offset) {
            return;
        }

        this._loadingRepository.startLoadingAction('Back');
        this.setNextSession(new SetNextSessionCommand({
            offset,
            resetSessionState: false
        }))
            .finally(() => this._loadingRepository.stopLoadingAction('Back'));
    }

    public skip(): void {
        const currentSession = this._playbackSessionRepository.currentSession;
        if (!currentSession) {
            return;
        }

        const offset = this._queueRepository.queue.findNextValidOffset(1);
        if (!offset) {
            return;
        }

        this._loadingRepository.startLoadingAction('Skip');

        this.setNextSession(new SetNextSessionCommand({
            offset,
            resetSessionState: false
        }))
            .finally(() => this._loadingRepository.stopLoadingAction('Skip'));
    }

    public setSessionEnded(): void {
        const currentSession = this._playbackSessionRepository.currentSession;
        if (!currentSession || currentSession.state.deviceId !== this._authenticationService.deviceId) {
            return;
        }

        const offset = this._queueRepository.queue.findNextValidOffset(1);
        if (!offset) {
            return;
        }

        this.setNextSession(new SetNextSessionCommand({
            offset,
            resetSessionState: true
        })).then();
    }

    private async setNextSession(command: SetNextSessionCommand): Promise<void> {
        try {
            const dto = await this._sessionClient.request('SetNextSession',
                    client => client.setNextSession(command),
                'Failed to set next session')
            this.next(dto);
        }
        catch (e) {
            //
        }
    }

    private next(dto: CurrentSessionUpdatedDto): void {
        const session = dto.session ? this._playbackSessionConverter.fromDto(dto.session) : null;
        const queue = this._queueConverter.fromDto(dto.queue);

        this._playbackSessionRepository.currentSession = session;
        this._queueRepository.setNextQueue(queue);
    }
}
