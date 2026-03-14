import type { PluginListenerHandle } from '@capacitor/core';

export interface NativeAudioPlugin {
  // Playback control
  setSource(options: { url: string }): Promise<void>;
  play(options: { fromTime?: number }): Promise<void>;
  pause(): Promise<void>;
  seek(options: { time: number }): Promise<void>;
  stop(): Promise<void>;

  // State queries
  getCurrentTime(): Promise<{ currentTime: number }>;
  getDuration(): Promise<{ duration: number }>;

  // Lock screen metadata
  setMetadata(options: {
    title: string;
    artist?: string;
    album?: string;
    duration?: number;
  }): Promise<void>;

  setNowPlayingPosition(options: {
    position: number;
    duration: number;
    playbackRate: number;
  }): Promise<void>;

  // Enable/disable lock screen skip controls
  setSkipControls(options: {
    hasNext: boolean;
    hasPrevious: boolean;
  }): Promise<void>;

  // Events from native -> JS
  addListener(eventName: 'playRequest', handler: () => void): Promise<PluginListenerHandle>;
  addListener(eventName: 'pauseRequest', handler: () => void): Promise<PluginListenerHandle>;
  addListener(eventName: 'seekRequest', handler: (data: { time: number }) => void): Promise<PluginListenerHandle>;
  addListener(eventName: 'nextTrackRequest', handler: () => void): Promise<PluginListenerHandle>;
  addListener(eventName: 'previousTrackRequest', handler: () => void): Promise<PluginListenerHandle>;
  addListener(eventName: 'timeUpdate', handler: (data: { currentTime: number }) => void): Promise<PluginListenerHandle>;
  addListener(eventName: 'durationChange', handler: (data: { duration: number }) => void): Promise<PluginListenerHandle>;
  addListener(eventName: 'ended', handler: () => void): Promise<PluginListenerHandle>;
  addListener(eventName: 'error', handler: (data: { message: string }) => void): Promise<PluginListenerHandle>;
}
