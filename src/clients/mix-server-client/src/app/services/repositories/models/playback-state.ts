export class PlaybackState {
  constructor(public currentTime: number,
              public deviceId: string | null | undefined,
              public playing: boolean) {
  }

  public copy(): PlaybackState {
    return new PlaybackState(this.currentTime, this.deviceId, this.playing);
  }
}
