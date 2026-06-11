import { GetUsersSessionsResponse, PlaybackSessionDto } from '../../generated-clients/mix-server-clients';
import { PlaybackSessionConverterService } from '../converters/playback-session-converter.service';
import { ApiResult, SessionApiService } from '../api.service';
import { HistoryRepositoryService } from './history-repository.service';
import { PlaybackSession } from './models/playback-session';

describe('HistoryRepositoryService', () => {
  let playbackSessionConverter: jasmine.SpyObj<PlaybackSessionConverterService>;
  let service: HistoryRepositoryService;
  let sessionClient: jasmine.SpyObj<SessionApiService>;

  beforeEach(() => {
    playbackSessionConverter = jasmine.createSpyObj<PlaybackSessionConverterService>('PlaybackSessionConverterService', ['fromDto']);
    sessionClient = jasmine.createSpyObj<SessionApiService>('SessionApiService', ['request']);

    service = new HistoryRepositoryService(playbackSessionConverter, sessionClient);
  });

  it('fetchRange_HistoryReturned_ConvertsSessions', async () => {
    // Arrange
    const dto = new PlaybackSessionDto();
    const session = { id: 'session-1' } as PlaybackSession;

    sessionClient.request.and.resolveTo(new ApiResult(undefined, new GetUsersSessionsResponse({ sessions: [dto] })));
    playbackSessionConverter.fromDto.and.returnValue(session);

    // Act
    const result = await service.fetchRange(0, 25);

    // Assert
    expect(result).toEqual([session]);
    expect(sessionClient.request).toHaveBeenCalledOnceWith(
      'LoadHistoryRange-0-25',
      jasmine.any(Function),
      'Failed to fetch history');
    expect(playbackSessionConverter.fromDto).toHaveBeenCalledOnceWith(dto);
  });

  it('fetchRange_HistoryMissing_ReturnsEmptyList', async () => {
    // Arrange
    sessionClient.request.and.resolveTo(new ApiResult(undefined, undefined));

    // Act
    const result = await service.fetchRange(0, 25);

    // Assert
    expect(result).toEqual([]);
    expect(playbackSessionConverter.fromDto).not.toHaveBeenCalled();
  });
});
