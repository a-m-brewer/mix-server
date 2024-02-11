import {PlaybackState} from "./playback-state";

export class PlaybackGranted extends PlaybackState {
  constructor(currentTime: number,
              deviceId: string | null | undefined,
              playing: boolean,
              public useDeviceCurrentTime: boolean) {
    super(currentTime, deviceId, playing);
  }
}
