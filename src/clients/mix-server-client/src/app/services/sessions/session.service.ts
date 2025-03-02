import {Injectable} from '@angular/core';
import {FileExplorerFileNode} from "../../main-content/file-explorer/models/file-explorer-file-node";
import {
    CurrentSessionUpdatedDto,
    SetCurrentSessionCommand,
    SetQueuePositionCommand
} from "../../generated-clients/mix-server-clients";
import {PlaybackSessionConverterService} from "../converters/playback-session-converter.service";
import {CurrentPlaybackSessionRepositoryService} from "../repositories/current-playback-session-repository.service";
import {QueueRepositoryService} from "../repositories/queue-repository.service";
import {QueueConverterService} from "../converters/queue-converter.service";
import {QueueApiService, SessionApiService} from "../api.service";

@Injectable({
    providedIn: 'root'
})
export class SessionService {

    constructor(
        private _playbackSessionConverter: PlaybackSessionConverterService,
        private _playbackSessionRepository: CurrentPlaybackSessionRepositoryService,
        private _sessionClient: SessionApiService,
        private _queueClient: QueueApiService,
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
        this._sessionClient.request('Back',
            client => client.back(), 'Failed to go back')
            .then(dto => this.next(dto));
    }

    public skip(): void {
        this._sessionClient.request('Skip',
            client => client.skip(), 'Failed to skip')
            .then(dto => this.next(dto));
    }

    public setSessionEnded(): void {
        this._sessionClient.request('SessionEnded',
            client => client.end(), 'Failed to end session')
            .then(dto => this.next(dto));
    }

    private next(dto: CurrentSessionUpdatedDto): void {
        const session = dto.session ? this._playbackSessionConverter.fromDto(dto.session) : null;
        const queue = this._queueConverter.fromDto(dto.queue);

        this._playbackSessionRepository.currentSession = session;
        this._queueRepository.setNextQueue(queue);
    }
}
