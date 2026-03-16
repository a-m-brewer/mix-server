import AVFoundation
import MediaPlayer
import UIKit

/// Native audio player using AVPlayer with background audio support and lock screen controls.
///
/// Uses `MPNowPlayingSession` to create an isolated Now Playing session tied to
/// our `AVPlayer`.  This prevents WKWebView from overriding the shared
/// `MPRemoteCommandCenter` / `MPNowPlayingInfoCenter` and ensures play/pause,
/// skip, and seek controls appear on the lock screen and Control Center on
/// iOS 16+ / iOS 26.
class NativeAudioPlayer: NSObject {
    private weak var plugin: NativeAudioPlugin?
    private var player: AVPlayer?
    private var playerItem: AVPlayerItem?
    private var timeObserver: Any?
    private var statusObservation: NSKeyValueObservation?
    private var durationObservation: NSKeyValueObservation?

    /// Session-scoped Now Playing session (iOS 16+).  Created once we have a
    /// player so that the system associates our remote-command handlers with
    /// this player rather than having WKWebView override them on the shared
    /// singletons.
    private var nowPlayingSession: AnyObject? // MPNowPlayingSession, stored as AnyObject for iOS 15 compat

    private var _currentTime: Double = 0
    private var _duration: Double = 0
    private var _title: String = ""

    var currentTime: Double { return _currentTime }
    var duration: Double { return _duration }

    /// Convenience accessor – returns the session-scoped info center when
    /// available, otherwise falls back to the global default.
    private var infoCenter: MPNowPlayingInfoCenter {
        if #available(iOS 16.0, *), let session = nowPlayingSession as? MPNowPlayingSession {
            return session.nowPlayingInfoCenter
        }
        return MPNowPlayingInfoCenter.default()
    }

    /// Convenience accessor – returns the session-scoped command center when
    /// available, otherwise falls back to the global shared instance.
    private var commandCenter: MPRemoteCommandCenter {
        if #available(iOS 16.0, *), let session = nowPlayingSession as? MPNowPlayingSession {
            return session.remoteCommandCenter
        }
        return MPRemoteCommandCenter.shared()
    }

    init(plugin: NativeAudioPlugin) {
        self.plugin = plugin
        super.init()
        setupAudioSession()
        // On iOS < 16 (no MPNowPlayingSession) register commands on the
        // shared command center immediately.  On iOS 16+ we defer until
        // setSource() creates the session-scoped command center.
        if #unavailable(iOS 16.0) {
            registerRemoteCommands(on: MPRemoteCommandCenter.shared())
        }
    }

    // MARK: - Audio Session

    /// Configure AVAudioSession for background playback.
    private func setupAudioSession() {
        do {
            let session = AVAudioSession.sharedInstance()
            try session.setCategory(.playback, mode: .default)
            try session.setActive(true)
        } catch {
            print("NativeAudioPlayer: Failed to set up audio session: \(error)")
        }

        DispatchQueue.main.async {
            UIApplication.shared.beginReceivingRemoteControlEvents()
        }

        NotificationCenter.default.addObserver(
            self,
            selector: #selector(handleInterruption(_:)),
            name: AVAudioSession.interruptionNotification,
            object: nil
        )
    }

    @objc private func handleInterruption(_ notification: Notification) {
        guard let userInfo = notification.userInfo,
              let typeValue = userInfo[AVAudioSessionInterruptionTypeKey] as? UInt,
              let type = AVAudioSession.InterruptionType(rawValue: typeValue) else {
            return
        }

        switch type {
        case .began:
            plugin?.notifyPauseRequest()
        case .ended:
            if let optionsValue = userInfo[AVAudioSessionInterruptionOptionKey] as? UInt {
                let options = AVAudioSession.InterruptionOptions(rawValue: optionsValue)
                if options.contains(.shouldResume) {
                    plugin?.notifyPlayRequest()
                }
            }
        @unknown default:
            break
        }
    }

    // MARK: - Now Playing Session & Remote Commands

    /// Create (or recreate) the `MPNowPlayingSession` tied to `player` and
    /// register all remote-command handlers on the **session-scoped** command
    /// center.  This isolates us from WKWebView overriding the shared
    /// `MPRemoteCommandCenter`.
    private func setupNowPlayingSession() {
        guard let player = player else { return }

        if #available(iOS 16.0, *) {
            let session = MPNowPlayingSession(players: [player])
            nowPlayingSession = session
            registerRemoteCommands(on: session.remoteCommandCenter)
            session.becomeActiveIfPossible()
        } else {
            // iOS 15 – commands already registered in init on the shared center
        }
    }

    /// Register all remote-command handlers on the provided command center.
    private func registerRemoteCommands(on cc: MPRemoteCommandCenter) {
        cc.playCommand.isEnabled = true
        cc.playCommand.addTarget { [weak self] _ in
            self?.player?.play()
            self?.updateNowPlayingPlaybackState(playing: true)
            self?.plugin?.notifyPlayRequest()
            return .success
        }

        cc.pauseCommand.isEnabled = true
        cc.pauseCommand.addTarget { [weak self] _ in
            self?.player?.pause()
            self?.updateNowPlayingPlaybackState(playing: false)
            self?.plugin?.notifyPauseRequest()
            return .success
        }

        cc.togglePlayPauseCommand.isEnabled = true
        cc.togglePlayPauseCommand.addTarget { [weak self] _ in
            guard let self = self else { return .commandFailed }
            if self.player?.rate == 0 {
                self.player?.play()
                self.updateNowPlayingPlaybackState(playing: true)
                self.plugin?.notifyPlayRequest()
            } else {
                self.player?.pause()
                self.updateNowPlayingPlaybackState(playing: false)
                self.plugin?.notifyPauseRequest()
            }
            return .success
        }

        cc.changePlaybackPositionCommand.isEnabled = true
        cc.changePlaybackPositionCommand.addTarget { [weak self] event in
            guard let positionEvent = event as? MPChangePlaybackPositionCommandEvent else {
                return .commandFailed
            }
            let time = CMTime(seconds: positionEvent.positionTime, preferredTimescale: CMTimeScale(NSEC_PER_SEC))
            self?.player?.seek(to: time)
            self?.plugin?.notifySeekRequest(time: positionEvent.positionTime)
            return .success
        }

        // Next/Previous — initially disabled, enabled via setSkipControls()
        cc.nextTrackCommand.isEnabled = false
        cc.nextTrackCommand.addTarget { [weak self] _ in
            self?.plugin?.notifyNextTrackRequest()
            return .success
        }

        cc.previousTrackCommand.isEnabled = false
        cc.previousTrackCommand.addTarget { [weak self] _ in
            self?.plugin?.notifyPreviousTrackRequest()
            return .success
        }
    }

    // MARK: - Playback Control

    func setSource(urlString: String) {
        guard let url = URL(string: urlString) else {
            plugin?.notifyError(message: "Invalid URL: \(urlString)")
            return
        }

        // Clean up previous player
        removeTimeObserver()
        removeItemObservers()

        playerItem = AVPlayerItem(url: url)

        if player == nil {
            player = AVPlayer(playerItem: playerItem!)
        } else {
            player?.replaceCurrentItem(with: playerItem!)
        }

        // (Re-)create the MPNowPlayingSession so iOS associates the commands
        // with this player instance.
        setupNowPlayingSession()

        setupItemObservers()
        setupTimeObserver()
    }

    func play(fromTime: Double?) {
        if let time = fromTime {
            let cmTime = CMTime(seconds: time, preferredTimescale: CMTimeScale(NSEC_PER_SEC))
            player?.seek(to: cmTime) { [weak self] _ in
                self?.player?.play()
                self?.updateNowPlayingPlaybackState(playing: true)
            }
        } else {
            player?.play()
            updateNowPlayingPlaybackState(playing: true)
        }
    }

    func pause() {
        player?.pause()
        updateNowPlayingPlaybackState(playing: false)
    }

    func seek(to time: Double) {
        let cmTime = CMTime(seconds: time, preferredTimescale: CMTimeScale(NSEC_PER_SEC))
        player?.seek(to: cmTime)
        _currentTime = time
        updateNowPlayingElapsedTime()
    }

    func stop() {
        player?.pause()
        player?.replaceCurrentItem(with: nil)
        removeTimeObserver()
        removeItemObservers()
        playerItem = nil
        _currentTime = 0
        _duration = 0
        clearNowPlaying()
    }

    // MARK: - Metadata & Now Playing

    func setMetadata(title: String, artist: String?, album: String?, duration: Double?) {
        _title = title
        if let dur = duration {
            _duration = dur
        }

        var nowPlayingInfo = infoCenter.nowPlayingInfo ?? [String: Any]()
        nowPlayingInfo[MPMediaItemPropertyTitle] = title
        if let artist = artist {
            nowPlayingInfo[MPMediaItemPropertyArtist] = artist
        }
        if let album = album {
            nowPlayingInfo[MPMediaItemPropertyAlbumTitle] = album
        }
        if let duration = duration {
            nowPlayingInfo[MPMediaItemPropertyPlaybackDuration] = duration
        } else if _duration > 0 {
            nowPlayingInfo[MPMediaItemPropertyPlaybackDuration] = _duration
        }
        nowPlayingInfo[MPNowPlayingInfoPropertyElapsedPlaybackTime] = _currentTime
        nowPlayingInfo[MPNowPlayingInfoPropertyPlaybackRate] = player?.rate ?? 0.0
        infoCenter.nowPlayingInfo = nowPlayingInfo
        infoCenter.playbackState = (player?.rate ?? 0) > 0 ? .playing : .paused
    }

    func setNowPlayingPosition(position: Double, duration: Double, playbackRate: Double) {
        _duration = duration
        var nowPlayingInfo = infoCenter.nowPlayingInfo ?? [String: Any]()
        nowPlayingInfo[MPNowPlayingInfoPropertyElapsedPlaybackTime] = position
        nowPlayingInfo[MPMediaItemPropertyPlaybackDuration] = duration
        nowPlayingInfo[MPNowPlayingInfoPropertyPlaybackRate] = playbackRate
        infoCenter.nowPlayingInfo = nowPlayingInfo
        infoCenter.playbackState = playbackRate > 0 ? .playing : .paused
    }

    func setSkipControls(hasNext: Bool, hasPrevious: Bool) {
        commandCenter.nextTrackCommand.isEnabled = hasNext
        commandCenter.previousTrackCommand.isEnabled = hasPrevious
    }

    private func updateNowPlayingPlaybackState(playing: Bool) {
        var nowPlayingInfo = infoCenter.nowPlayingInfo ?? [String: Any]()
        nowPlayingInfo[MPNowPlayingInfoPropertyElapsedPlaybackTime] = _currentTime
        nowPlayingInfo[MPNowPlayingInfoPropertyPlaybackRate] = playing ? 1.0 : 0.0
        if nowPlayingInfo[MPMediaItemPropertyTitle] == nil && !_title.isEmpty {
            nowPlayingInfo[MPMediaItemPropertyTitle] = _title
        }
        if _duration > 0 {
            nowPlayingInfo[MPMediaItemPropertyPlaybackDuration] = _duration
        }
        infoCenter.nowPlayingInfo = nowPlayingInfo
        infoCenter.playbackState = playing ? .playing : .paused
    }

    private func updateNowPlayingElapsedTime() {
        var nowPlayingInfo = infoCenter.nowPlayingInfo ?? [String: Any]()
        nowPlayingInfo[MPNowPlayingInfoPropertyElapsedPlaybackTime] = _currentTime
        infoCenter.nowPlayingInfo = nowPlayingInfo
    }

    private func updateNowPlayingWithDuration(_ duration: Double) {
        var nowPlayingInfo = infoCenter.nowPlayingInfo ?? [String: Any]()
        nowPlayingInfo[MPMediaItemPropertyPlaybackDuration] = duration
        nowPlayingInfo[MPNowPlayingInfoPropertyElapsedPlaybackTime] = _currentTime
        nowPlayingInfo[MPNowPlayingInfoPropertyPlaybackRate] = player?.rate ?? 0.0
        if nowPlayingInfo[MPMediaItemPropertyTitle] == nil && !_title.isEmpty {
            nowPlayingInfo[MPMediaItemPropertyTitle] = _title
        }
        infoCenter.nowPlayingInfo = nowPlayingInfo
    }

    private func clearNowPlaying() {
        infoCenter.nowPlayingInfo = nil
        infoCenter.playbackState = .stopped
    }

    // MARK: - Observers

    private var _timeObserverTickCount: Int = 0
    private let nowPlayingUpdateInterval: Int = 10 // every 5 seconds

    private func setupTimeObserver() {
        _timeObserverTickCount = 0
        let interval = CMTime(seconds: 0.5, preferredTimescale: CMTimeScale(NSEC_PER_SEC))
        timeObserver = player?.addPeriodicTimeObserver(forInterval: interval, queue: .main) { [weak self] time in
            guard let self = self else { return }
            let seconds = CMTimeGetSeconds(time)
            if seconds.isFinite {
                self._currentTime = seconds
                self.plugin?.notifyTimeUpdate(currentTime: seconds)

                self._timeObserverTickCount += 1
                if self._timeObserverTickCount >= self.nowPlayingUpdateInterval {
                    self._timeObserverTickCount = 0
                    self.updateNowPlayingElapsedTime()
                }
            }
        }
    }

    private func removeTimeObserver() {
        if let observer = timeObserver {
            player?.removeTimeObserver(observer)
            timeObserver = nil
        }
    }

    private func setupItemObservers() {
        guard let item = playerItem else { return }

        statusObservation = item.observe(\.status, options: [.new]) { [weak self] item, _ in
            switch item.status {
            case .failed:
                let message = item.error?.localizedDescription ?? "Unknown playback error"
                self?.plugin?.notifyError(message: message)
            case .readyToPlay:
                let duration = CMTimeGetSeconds(item.duration)
                if duration.isFinite && duration != self?._duration {
                    self?._duration = duration
                    self?.updateNowPlayingWithDuration(duration)
                    self?.plugin?.notifyDurationChange(duration: duration)
                }
            default:
                break
            }
        }

        durationObservation = item.observe(\.duration, options: [.new]) { [weak self] item, _ in
            let duration = CMTimeGetSeconds(item.duration)
            if duration.isFinite && duration != self?._duration {
                self?._duration = duration
                self?.updateNowPlayingWithDuration(duration)
                self?.plugin?.notifyDurationChange(duration: duration)
            }
        }

        NotificationCenter.default.addObserver(
            self,
            selector: #selector(playerItemDidPlayToEnd(_:)),
            name: .AVPlayerItemDidPlayToEndTime,
            object: item
        )
    }

    private func removeItemObservers() {
        statusObservation?.invalidate()
        statusObservation = nil
        durationObservation?.invalidate()
        durationObservation = nil
        NotificationCenter.default.removeObserver(self, name: .AVPlayerItemDidPlayToEndTime, object: playerItem)
    }

    @objc private func playerItemDidPlayToEnd(_ notification: Notification) {
        plugin?.notifyEnded()
    }

    deinit {
        removeTimeObserver()
        removeItemObservers()
        NotificationCenter.default.removeObserver(self)
    }
}
