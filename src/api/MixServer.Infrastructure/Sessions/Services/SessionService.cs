using Microsoft.Extensions.Logging;
using MixServer.Domain.Callbacks;
using MixServer.Domain.Exceptions;
using MixServer.Domain.FileExplorer.Services;
using MixServer.Domain.Sessions.Entities;
using MixServer.Domain.Sessions.Repositories;
using MixServer.Domain.Sessions.Requests;
using MixServer.Domain.Sessions.Services;
using MixServer.Domain.Users.Repositories;
using MixServer.Domain.Utilities;
using MixServer.Infrastructure.EF.Entities;
using MixServer.Infrastructure.Users.Repository;

namespace MixServer.Infrastructure.Sessions.Services;

public class SessionService : ISessionService
{
    private readonly ICallbackService _callbackService;
    private readonly ICurrentDeviceRepository _currentDeviceRepository;
    private readonly ICurrentUserRepository _currentUserRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IFileService _fileService;
    private readonly ILogger<SessionService> _logger;
    private readonly IPlaybackSessionRepository _playbackSessionRepository;
    private readonly IPlaybackTrackingService _playbackTrackingService;
    private readonly IUserRepository _userRepository;

    public SessionService(
        ICallbackService callbackService,
        ICurrentDeviceRepository currentDeviceRepository,
        ICurrentUserRepository currentUserRepository,
        IDateTimeProvider dateTimeProvider,
        IFileService fileService,
        ILogger<SessionService> logger,
        IPlaybackSessionRepository playbackSessionRepository,
        IPlaybackTrackingService playbackTrackingService,
        IUserRepository userRepository)
    {
        _callbackService = callbackService;
        _currentDeviceRepository = currentDeviceRepository;
        _currentUserRepository = currentUserRepository;
        _dateTimeProvider = dateTimeProvider;
        _fileService = fileService;
        _logger = logger;
        _playbackSessionRepository = playbackSessionRepository;
        _playbackTrackingService = playbackTrackingService;
        _userRepository = userRepository;
    }

    public async Task LoadPlaybackStateAsync()
    {
        if (_playbackTrackingService.IsTracking(_currentUserRepository.CurrentUserId))
        {
            _logger.LogDebug("Skipping loading playback state as it is already being tracked");
            return;
        }

        var session = await GetCurrentPlaybackSessionOrDefaultAsync();

        if (session == null)
        {
            _logger.LogDebug("Skipping loading playback state as there is currently no playback session to track");
            return;
        }

        _playbackTrackingService.UpdateSessionState(session);
    }

    public async Task<PlaybackSession> AddOrUpdateSessionAsync(IAddOrUpdateSessionRequest request)
    {
        await _currentUserRepository.LoadAllPlaybackSessionsAsync();
        var user = _currentUserRepository.CurrentUser;
        
        var session = user.PlaybackSessions.SingleOrDefault(s => s.AbsolutePath == request.AbsoluteFilePath);

        if (session == null)
        {
            session = new PlaybackSession
            {
                Id = Guid.NewGuid(),
                AbsolutePath = request.AbsoluteFilePath,
                LastPlayed = _dateTimeProvider.UtcNow,
                UserId = user.Id,
                CurrentTime = TimeSpan.Zero
            };

            await _playbackSessionRepository.AddAsync(session);
            user.PlaybackSessions.Add(session);
        }
        else
        {
            session.LastPlayed = _dateTimeProvider.UtcNow;
        }

        session.DeviceId = _playbackTrackingService.TryGet(user.Id, out var state) && state.HasDevice
            ? state.DeviceIdOrThrow
            : _currentDeviceRepository.DeviceId;

        _playbackTrackingService.UpdateSessionState(session);
        
        user.CurrentPlaybackSession = session;

        SetSessionNonMappedProperties(session);
        
        _callbackService.InvokeCallbackOnSaved(c => c.CurrentSessionUpdated(session.UserId, session));
        return session;
    }

    public void ClearUsersCurrentSession()
    {
        var user = _currentUserRepository.CurrentUser;
        
        if (user.CurrentPlaybackSession != null)
        {
            _playbackTrackingService.ClearSession(user.Id);
        }
        
        user.CurrentPlaybackSession = null;
        
        _callbackService.InvokeCallbackOnSaved(c => c.CurrentSessionUpdated(user.Id, null));
    }

    public async Task<PlaybackSession> GetPlaybackSessionByIdAsync(Guid id, string username)
    {
        var user = await _userRepository.GetUserAsync(username);

        var session = await _playbackSessionRepository.GetAsync(id);

        if (session.UserId != user.Id)
        {
            throw new ForbiddenRequestException();
        }

        SetSessionNonMappedProperties(session);
        
        return session;
    }

    public async Task<PlaybackSession> GetCurrentPlaybackSessionWithFileAsync()
    {
        var session = await GetCurrentPlaybackSessionAsync();
        
        SetSessionNonMappedProperties(session);

        return session;
    }

    public async Task<PlaybackSession> GetCurrentPlaybackSessionAsync()
    {
        var session = await GetCurrentPlaybackSessionOrDefaultAsync();

        if (session == null)
        {
            throw new NotFoundException(nameof(DbUser), nameof(DbUser.CurrentPlaybackSession));
        }

        return session;
    }

    public async Task<PlaybackSession?> GetCurrentPlaybackSessionOrDefaultAsync()
    {
        await _currentUserRepository.LoadCurrentPlaybackSessionAsync();
        var user = _currentUserRepository.CurrentUser;

        var session = user.CurrentPlaybackSession;

        return session;
    }

    public async Task<List<PlaybackSession>> GetUsersPlaybackSessionHistoryAsync(int startIndex, int pageSize)
    {
        await _currentUserRepository.LoadPagedPlaybackSessionsAsync(startIndex, pageSize);
        var user = _currentUserRepository.CurrentUser;

        var sessions = user.PlaybackSessions;

        foreach (var session in sessions)
        {
            SetSessionNonMappedProperties(session);
        }

        return sessions;
    }

    private void SetSessionNonMappedProperties(
        IPlaybackSession session)
    {
        var parentFolderPath = session.GetParentFolderPathOrThrow();
        var parent = _fileService.GetUnpopulatedFolder(parentFolderPath);

        var currentNode = _fileService.GetFile(session.AbsolutePath, parent);

        _playbackTrackingService.Populate(session);

        session.File = currentNode;
    }
}