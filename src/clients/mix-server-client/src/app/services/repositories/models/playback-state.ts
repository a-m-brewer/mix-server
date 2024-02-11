export class PlaybackState {
  constructor(public currentTime: number,
              public deviceId: string | null | undefined,
              public playing: boolean) {
  }
}
