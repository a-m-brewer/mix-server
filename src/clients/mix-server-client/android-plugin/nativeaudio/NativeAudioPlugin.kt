package com.mixserver.app.plugins.nativeaudio

import com.getcapacitor.JSObject
import com.getcapacitor.Plugin
import com.getcapacitor.PluginCall
import com.getcapacitor.PluginMethod
import com.getcapacitor.annotation.CapacitorPlugin

@CapacitorPlugin(name = "NativeAudio")
class NativeAudioPlugin : Plugin() {

    private lateinit var audioPlayer: NativeAudioPlayer

    override fun load() {
        audioPlayer = NativeAudioPlayer(context, this)
    }

    @PluginMethod
    fun setSource(call: PluginCall) {
        val url = call.getString("url")
        if (url == null) {
            call.reject("Missing 'url' parameter")
            return
        }
        audioPlayer.setSource(url)
        call.resolve()
    }

    @PluginMethod
    fun play(call: PluginCall) {
        val fromTime = call.getDouble("fromTime")
        audioPlayer.play(fromTime)
        call.resolve()
    }

    @PluginMethod
    fun pause(call: PluginCall) {
        audioPlayer.pause()
        call.resolve()
    }

    @PluginMethod
    fun seek(call: PluginCall) {
        val time = call.getDouble("time")
        if (time == null) {
            call.reject("Missing 'time' parameter")
            return
        }
        audioPlayer.seek(time)
        call.resolve()
    }

    @PluginMethod
    fun stop(call: PluginCall) {
        audioPlayer.stop()
        call.resolve()
    }

    @PluginMethod
    fun getCurrentTime(call: PluginCall) {
        val result = JSObject()
        result.put("currentTime", audioPlayer.currentTime)
        call.resolve(result)
    }

    @PluginMethod
    fun getDuration(call: PluginCall) {
        val result = JSObject()
        result.put("duration", audioPlayer.duration)
        call.resolve(result)
    }

    @PluginMethod
    fun setMetadata(call: PluginCall) {
        val title = call.getString("title") ?: ""
        val artist = call.getString("artist")
        val album = call.getString("album")
        val duration = call.getDouble("duration")
        audioPlayer.setMetadata(title, artist, album, duration)
        call.resolve()
    }

    @PluginMethod
    fun setNowPlayingPosition(call: PluginCall) {
        val position = call.getDouble("position")
        val duration = call.getDouble("duration")
        val playbackRate = call.getDouble("playbackRate")
        if (position == null || duration == null || playbackRate == null) {
            call.reject("Missing required parameters")
            return
        }
        audioPlayer.setNowPlayingPosition(position, duration, playbackRate)
        call.resolve()
    }

    @PluginMethod
    fun setSkipControls(call: PluginCall) {
        val hasNext = call.getBoolean("hasNext") ?: false
        val hasPrevious = call.getBoolean("hasPrevious") ?: false
        audioPlayer.setSkipControls(hasNext, hasPrevious)
        call.resolve()
    }

    // Event forwarding from NativeAudioPlayer

    fun notifyPlayRequest() {
        notifyListeners("playRequest", JSObject())
    }

    fun notifyPauseRequest() {
        notifyListeners("pauseRequest", JSObject())
    }

    fun notifySeekRequest(time: Double) {
        val data = JSObject()
        data.put("time", time)
        notifyListeners("seekRequest", data)
    }

    fun notifyNextTrackRequest() {
        notifyListeners("nextTrackRequest", JSObject())
    }

    fun notifyPreviousTrackRequest() {
        notifyListeners("previousTrackRequest", JSObject())
    }

    fun notifyTimeUpdate(currentTime: Double) {
        val data = JSObject()
        data.put("currentTime", currentTime)
        notifyListeners("timeUpdate", data)
    }

    fun notifyDurationChange(duration: Double) {
        val data = JSObject()
        data.put("duration", duration)
        notifyListeners("durationChange", data)
    }

    fun notifyEnded() {
        notifyListeners("ended", JSObject())
    }

    fun notifyError(message: String) {
        val data = JSObject()
        data.put("message", message)
        notifyListeners("error", data)
    }

    override fun handleOnDestroy() {
        audioPlayer.release()
        super.handleOnDestroy()
    }
}
