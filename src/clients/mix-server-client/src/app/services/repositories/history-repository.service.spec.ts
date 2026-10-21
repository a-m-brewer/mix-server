import { beforeEach, describe, expect, it, type MockedObject, vi } from "vitest";
import { GetUsersSessionsResponse, PlaybackSessionDto } from '../../generated-clients/mix-server-clients';
import { PlaybackSessionConverterService } from '../converters/playback-session-converter.service';
import { ApiResult, SessionApiService } from '../api.service';
import { HistoryRepositoryService } from './history-repository.service';
import { PlaybackSession } from './models/playback-session';

describe('HistoryRepositoryService', () => {
    let playbackSessionConverter: MockedObject<PlaybackSessionConverterService>;
    let service: HistoryRepositoryService;
    let sessionClient: MockedObject<SessionApiService>;

    beforeEach(() => {
        playbackSessionConverter = {
            fromDto: vi.fn().mockName("PlaybackSessionConverterService.fromDto")
        } as MockedObject<PlaybackSessionConverterService>;
        sessionClient = {
            request: vi.fn().mockName("SessionApiService.request")
        } as MockedObject<SessionApiService>;

        service = new HistoryRepositoryService(playbackSessionConverter, sessionClient);
    });

    it('fetchRange_HistoryReturned_ConvertsSessions', async () => {
        // Arrange
        const dto = new PlaybackSessionDto();
        const session = { id: 'session-1' } as PlaybackSession;

        sessionClient.request.mockResolvedValue(new ApiResult(undefined, new GetUsersSessionsResponse({ sessions: [dto] })));
        playbackSessionConverter.fromDto.mockReturnValue(session);

        // Act
        const result = await service.fetchRange(0, 25);

        // Assert
        expect(result).toEqual([session]);
        expect(sessionClient.request).toHaveBeenCalledTimes(1);
        expect(sessionClient.request).toHaveBeenCalledWith('LoadHistoryRange-0-25', expect.any(Function), 'Failed to fetch history');
        expect(playbackSessionConverter.fromDto).toHaveBeenCalledTimes(1);
        expect(playbackSessionConverter.fromDto).toHaveBeenCalledWith(dto);
    });

    it('fetchRange_HistoryMissing_ReturnsEmptyList', async () => {
        // Arrange
        sessionClient.request.mockResolvedValue(new ApiResult(undefined, undefined));

        // Act
        const result = await service.fetchRange(0, 25);

        // Assert
        expect(result).toEqual([]);
        expect(playbackSessionConverter.fromDto).not.toHaveBeenCalled();
    });
});
