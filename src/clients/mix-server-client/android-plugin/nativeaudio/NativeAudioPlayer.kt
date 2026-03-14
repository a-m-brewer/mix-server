package com.mixserver.app.plugins.nativeaudio

import android.content.Context
import android.os.Handler
import android.os.Looper
import androidx.media3.common.MediaItem
import androidx.media3.common.MediaMetadata
import androidx.media3.common.PlaybackException
import androidx.media3.common.Player
import androidx.media3.exoplayer.ExoPlayer
import androidx.media3.session.MediaSession

/**
 * Native audio player using ExoPlayer (Media3) with MediaSession for lock screen controls.
 *
 * ExoPlayer natively supports HLS streaming. MediaSession provides:
 * - Lock screen controls via media notification
 * - Bluetooth/headphone button support
 * - Background playback via foreground service notification
 */
class NativeAudioPlayer(
    private val context: Context,
    private val plugin: NativeAudioPlugin
) {
    private var exoPlayer: ExoPlayer? = null
    private var mediaSession: MediaSession? = null
    private val handler = Handler(Looper.getMainLooper())
    private var timeUpdateRunnable: Runnable? = null

    private var _currentTime: Double = 0.0
    private var _duration: Double = 0.0
    private var _hasNext: Boolean = false
    private var _hasPrevious: Boolean = false

    val currentTime: Double get() = _currentTime
    val duration: Double get() = _duration

    init {
        initializePlayer()
    }

    private fun initializePlayer() {
        handler.post {
            exoPlayer = ExoPlayer.Builder(context).build().also { player ->
                player.addListener(playerListener)

                // Create MediaSession for lock screen / notification controls
                mediaSession = MediaSession.Builder(context, player)
                    .setCallback(mediaSessionCallback)
                    .build()
            }
        }
    }

    private val playerListener = object : Player.Listener {
        override fun onPlaybackStateChanged(playbackState: Int) {
            when (playbackState) {
                Player.STATE_READY -> {
                    val durationMs = exoPlayer?.duration ?: 0L
                    if (durationMs > 0) {
                        val durationSec = durationMs / 1000.0
                        if (durationSec != _duration) {
                            _duration = durationSec
                            plugin.notifyDurationChange(durationSec)
                        }
                    }
                }
                Player.STATE_ENDED -> {
                    stopTimeUpdates()
                    plugin.notifyEnded()
                }
                else -> {}
            }
        }

        override fun onIsPlayingChanged(isPlaying: Boolean) {
            if (isPlaying) {
                startTimeUpdates()
            } else {
                stopTimeUpdates()
            }
        }

        override fun onPlayerError(error: PlaybackException) {
            plugin.notifyError(error.message ?: "Unknown playback error")
        }
    }

    private val mediaSessionCallback = object : MediaSession.Callback {
        override fun onConnect(
            session: MediaSession,
            controller: MediaSession.ControllerInfo
        ): MediaSession.ConnectionResult {
            val availableSessionCommands = MediaSession.ConnectionResult.DEFAULT_SESSION_AND_LIBRARY_COMMANDS

            return MediaSession.ConnectionResult.AcceptedResultBuilder(session)
                .setAvailableSessionCommands(availableSessionCommands)
                .build()
        }
    }

    // MARK: - Playback Control

    fun setSource(urlString: String) {
        handler.post {
            val mediaItem = MediaItem.fromUri(urlString)
            exoPlayer?.setMediaItem(mediaItem)
            exoPlayer?.prepare()
        }
    }

    fun play(fromTime: Double?) {
        handler.post {
            if (fromTime != null) {
                exoPlayer?.seekTo((fromTime * 1000).toLong())
            }
            exoPlayer?.play()
        }
    }

    fun pause() {
        handler.post {
            exoPlayer?.pause()
            plugin.notifyPauseRequest()
        }
    }

    fun seek(time: Double) {
        handler.post {
            exoPlayer?.seekTo((time * 1000).toLong())
            _currentTime = time
        }
    }

    fun stop() {
        handler.post {
            stopTimeUpdates()
            exoPlayer?.stop()
            exoPlayer?.clearMediaItems()
            _currentTime = 0.0
            _duration = 0.0
        }
    }

    // MARK: - Metadata

    fun setMetadata(title: String, artist: String?, album: String?, duration: Double?) {
        if (duration != null) {
            _duration = duration
        }

        handler.post {
            val metadata = MediaMetadata.Builder()
                .setTitle(title)
                .apply {
                    if (artist != null) setArtist(artist)
                    if (album != null) setAlbumTitle(album)
                }
                .build()

            // Update the current media item's metadata
            val currentItem = exoPlayer?.currentMediaItem
            if (currentItem != null) {
                val updatedItem = currentItem.buildUpon()
                    .setMediaMetadata(metadata)
                    .build()
                exoPlayer?.replaceMediaItem(0, updatedItem)
            }
        }
    }

    fun setNowPlayingPosition(position: Double, duration: Double, playbackRate: Double) {
        _duration = duration
        // ExoPlayer + MediaSession handle now playing info automatically
    }

    fun setSkipControls(hasNext: Boolean, hasPrevious: Boolean) {
        _hasNext = hasNext
        _hasPrevious = hasPrevious
        // MediaSession handles skip button visibility based on available commands
    }

    // MARK: - Time Updates

    private fun startTimeUpdates() {
        stopTimeUpdates()
        timeUpdateRunnable = object : Runnable {
            override fun run() {
                val positionMs = exoPlayer?.currentPosition ?: 0L
                val positionSec = positionMs / 1000.0
                _currentTime = positionSec
                plugin.notifyTimeUpdate(positionSec)
                handler.postDelayed(this, 500)
            }
        }
        handler.post(timeUpdateRunnable!!)
    }

    private fun stopTimeUpdates() {
        timeUpdateRunnable?.let { handler.removeCallbacks(it) }
        timeUpdateRunnable = null
    }

    // MARK: - Cleanup

    fun release() {
        handler.post {
            stopTimeUpdates()
            mediaSession?.release()
            mediaSession = null
            exoPlayer?.release()
            exoPlayer = null
        }
    }
}
