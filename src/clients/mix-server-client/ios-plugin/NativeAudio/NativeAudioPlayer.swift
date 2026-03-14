import AVFoundation
import MediaPlayer

/// Native audio player using AVPlayer with background audio support and lock screen controls.
/// This is the core class that solves the iOS JS suspension issue:
/// - AVAudioSession category `.playback` keeps the audio session alive in background
/// - MPRemoteCommandCenter handlers fire in the native process even when WKWebView JS is suspended
/// - When the user taps play on the lock screen, the native handler starts playback and wakes JS
class NativeAudioPlayer: NSObject {
    private weak var plugin: NativeAudioPlugin?
    private var player: AVPlayer?
    private var playerItem: AVPlayerItem?
    private var timeObserver: Any?
    private var statusObservation: NSKeyValueObservation?
    private var durationObservation: NSKeyValueObservation?

    private var _currentTime: Double = 0
    private var _duration: Double = 0
    private var _title: String = ""

    var currentTime: Double { return _currentTime }
    var duration: Double { return _duration }

    init(plugin: NativeAudioPlugin) {
        self.plugin = plugin
        super.init()
        setupAudioSession()
        setupRemoteCommands()
    }

    // MARK: - Audio Session

    /// Configure AVAudioSession for background playback.
    /// Category `.playback` tells iOS this app plays audio that should continue in the background.
    /// Combined with the `audio` UIBackgroundMode in Info.plist, this keeps the native process
    /// alive and MPRemoteCommandCenter handlers responsive even when the screen is locked.
    private func setupAudioSession() {
        do {
            let session = AVAudioSession.sharedInstance()
            try session.setCategory(.playback, mode: .default)
            try session.setActive(true)
        } catch {
            print("NativeAudioPlayer: Failed to set up audio session: \(error)")
        }

        // Handle audio interruptions (phone calls, Siri, etc.)
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

    // MARK: - Remote Command Center (Lock Screen Controls)

    /// Register handlers with MPRemoteCommandCenter.
    /// These handlers fire in the NATIVE process even when WKWebView JS is suspended.
    /// This is the key fix for the iOS lock screen issue.
    private func setupRemoteCommands() {
        let commandCenter = MPRemoteCommandCenter.shared()

        commandCenter.playCommand.isEnabled = true
        commandCenter.playCommand.addTarget { [weak self] _ in
            self?.player?.play()
            self?.updateNowPlayingPlaybackState(playing: true)
            self?.plugin?.notifyPlayRequest()
            return .success
        }

        commandCenter.pauseCommand.isEnabled = true
        commandCenter.pauseCommand.addTarget { [weak self] _ in
            self?.player?.pause()
            self?.updateNowPlayingPlaybackState(playing: false)
            self?.plugin?.notifyPauseRequest()
            return .success
        }

        commandCenter.changePlaybackPositionCommand.isEnabled = true
        commandCenter.changePlaybackPositionCommand.addTarget { [weak self] event in
            guard let positionEvent = event as? MPChangePlaybackPositionCommandEvent else {
                return .commandFailed
            }
            let time = CMTime(seconds: positionEvent.positionTime, preferredTimescale: CMTimeScale(NSEC_PER_SEC))
            self?.player?.seek(to: time)
            self?.plugin?.notifySeekRequest(time: positionEvent.positionTime)
            return .success
        }

        // Next/Previous — initially disabled, enabled via setSkipControls()
        commandCenter.nextTrackCommand.isEnabled = false
        commandCenter.nextTrackCommand.addTarget { [weak self] _ in
            self?.plugin?.notifyNextTrackRequest()
            return .success
        }

        commandCenter.previousTrackCommand.isEnabled = false
        commandCenter.previousTrackCommand.addTarget { [weak self] _ in
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

        var nowPlayingInfo = MPNowPlayingInfoCenter.default().nowPlayingInfo ?? [String: Any]()
        nowPlayingInfo[MPMediaItemPropertyTitle] = title
        if let artist = artist {
            nowPlayingInfo[MPMediaItemPropertyArtist] = artist
        }
        if let album = album {
            nowPlayingInfo[MPMediaItemPropertyAlbumTitle] = album
        }
        if let duration = duration {
            nowPlayingInfo[MPMediaItemPropertyPlaybackDuration] = duration
        }
        nowPlayingInfo[MPNowPlayingInfoPropertyElapsedPlaybackTime] = _currentTime
        nowPlayingInfo[MPNowPlayingInfoPropertyPlaybackRate] = player?.rate ?? 0.0
        MPNowPlayingInfoCenter.default().nowPlayingInfo = nowPlayingInfo
    }

    func setNowPlayingPosition(position: Double, duration: Double, playbackRate: Double) {
        _duration = duration
        var nowPlayingInfo = MPNowPlayingInfoCenter.default().nowPlayingInfo ?? [String: Any]()
        nowPlayingInfo[MPNowPlayingInfoPropertyElapsedPlaybackTime] = position
        nowPlayingInfo[MPMediaItemPropertyPlaybackDuration] = duration
        nowPlayingInfo[MPNowPlayingInfoPropertyPlaybackRate] = playbackRate
        MPNowPlayingInfoCenter.default().nowPlayingInfo = nowPlayingInfo
    }

    func setSkipControls(hasNext: Bool, hasPrevious: Bool) {
        let commandCenter = MPRemoteCommandCenter.shared()
        commandCenter.nextTrackCommand.isEnabled = hasNext
        commandCenter.previousTrackCommand.isEnabled = hasPrevious
    }

    private func updateNowPlayingPlaybackState(playing: Bool) {
        var nowPlayingInfo = MPNowPlayingInfoCenter.default().nowPlayingInfo ?? [String: Any]()
        nowPlayingInfo[MPNowPlayingInfoPropertyElapsedPlaybackTime] = _currentTime
        nowPlayingInfo[MPNowPlayingInfoPropertyPlaybackRate] = playing ? 1.0 : 0.0
        MPNowPlayingInfoCenter.default().nowPlayingInfo = nowPlayingInfo
    }

    private func updateNowPlayingElapsedTime() {
        var nowPlayingInfo = MPNowPlayingInfoCenter.default().nowPlayingInfo ?? [String: Any]()
        nowPlayingInfo[MPNowPlayingInfoPropertyElapsedPlaybackTime] = _currentTime
        MPNowPlayingInfoCenter.default().nowPlayingInfo = nowPlayingInfo
    }

    private func clearNowPlaying() {
        MPNowPlayingInfoCenter.default().nowPlayingInfo = nil
    }

    // MARK: - Observers

    private func setupTimeObserver() {
        let interval = CMTime(seconds: 0.5, preferredTimescale: CMTimeScale(NSEC_PER_SEC))
        timeObserver = player?.addPeriodicTimeObserver(forInterval: interval, queue: .main) { [weak self] time in
            guard let self = self else { return }
            let seconds = CMTimeGetSeconds(time)
            if seconds.isFinite {
                self._currentTime = seconds
                self.plugin?.notifyTimeUpdate(currentTime: seconds)
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
