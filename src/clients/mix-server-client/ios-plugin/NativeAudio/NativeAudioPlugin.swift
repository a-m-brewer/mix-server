import Foundation
import Capacitor

/// Capacitor plugin bridge for native audio playback.
/// Register in the app's AppDelegate or via the Capacitor plugin loader.
@objc(NativeAudioPlugin)
public class NativeAudioPlugin: CAPPlugin, CAPBridgedPlugin {
    public let identifier = "NativeAudioPlugin"
    public let jsName = "NativeAudio"
    public let pluginMethods: [CAPPluginMethod] = [
        CAPPluginMethod(name: "setSource", returnType: CAPPluginReturnPromise),
        CAPPluginMethod(name: "play", returnType: CAPPluginReturnPromise),
        CAPPluginMethod(name: "pause", returnType: CAPPluginReturnPromise),
        CAPPluginMethod(name: "seek", returnType: CAPPluginReturnPromise),
        CAPPluginMethod(name: "stop", returnType: CAPPluginReturnPromise),
        CAPPluginMethod(name: "getCurrentTime", returnType: CAPPluginReturnPromise),
        CAPPluginMethod(name: "getDuration", returnType: CAPPluginReturnPromise),
        CAPPluginMethod(name: "setMetadata", returnType: CAPPluginReturnPromise),
        CAPPluginMethod(name: "setNowPlayingPosition", returnType: CAPPluginReturnPromise),
        CAPPluginMethod(name: "setSkipControls", returnType: CAPPluginReturnPromise),
    ]

    private lazy var player: NativeAudioPlayer = {
        return NativeAudioPlayer(plugin: self)
    }()

    @objc func setSource(_ call: CAPPluginCall) {
        guard let url = call.getString("url") else {
            call.reject("Missing 'url' parameter")
            return
        }
        player.setSource(urlString: url)
        call.resolve()
    }

    @objc func play(_ call: CAPPluginCall) {
        let fromTime = call.getDouble("fromTime")
        player.play(fromTime: fromTime)
        call.resolve()
    }

    @objc func pause(_ call: CAPPluginCall) {
        player.pause()
        call.resolve()
    }

    @objc func seek(_ call: CAPPluginCall) {
        guard let time = call.getDouble("time") else {
            call.reject("Missing 'time' parameter")
            return
        }
        player.seek(to: time)
        call.resolve()
    }

    @objc func stop(_ call: CAPPluginCall) {
        player.stop()
        call.resolve()
    }

    @objc func getCurrentTime(_ call: CAPPluginCall) {
        call.resolve(["currentTime": player.currentTime])
    }

    @objc func getDuration(_ call: CAPPluginCall) {
        call.resolve(["duration": player.duration])
    }

    @objc func setMetadata(_ call: CAPPluginCall) {
        let title = call.getString("title") ?? ""
        let artist = call.getString("artist")
        let album = call.getString("album")
        let duration = call.getDouble("duration")
        player.setMetadata(title: title, artist: artist, album: album, duration: duration)
        call.resolve()
    }

    @objc func setNowPlayingPosition(_ call: CAPPluginCall) {
        guard let position = call.getDouble("position"),
              let duration = call.getDouble("duration"),
              let playbackRate = call.getDouble("playbackRate") else {
            call.reject("Missing required parameters")
            return
        }
        player.setNowPlayingPosition(position: position, duration: duration, playbackRate: playbackRate)
        call.resolve()
    }

    @objc func setSkipControls(_ call: CAPPluginCall) {
        let hasNext = call.getBool("hasNext") ?? false
        let hasPrevious = call.getBool("hasPrevious") ?? false
        player.setSkipControls(hasNext: hasNext, hasPrevious: hasPrevious)
        call.resolve()
    }

    // MARK: - Event forwarding from NativeAudioPlayer

    func notifyPlayRequest() {
        notifyListeners("playRequest", data: [:])
    }

    func notifyPauseRequest() {
        notifyListeners("pauseRequest", data: [:])
    }

    func notifySeekRequest(time: Double) {
        notifyListeners("seekRequest", data: ["time": time])
    }

    func notifyNextTrackRequest() {
        notifyListeners("nextTrackRequest", data: [:])
    }

    func notifyPreviousTrackRequest() {
        notifyListeners("previousTrackRequest", data: [:])
    }

    func notifyTimeUpdate(currentTime: Double) {
        notifyListeners("timeUpdate", data: ["currentTime": currentTime])
    }

    func notifyDurationChange(duration: Double) {
        notifyListeners("durationChange", data: ["duration": duration])
    }

    func notifyEnded() {
        notifyListeners("ended", data: [:])
    }

    func notifyError(message: String) {
        notifyListeners("error", data: ["message": message])
    }
}
