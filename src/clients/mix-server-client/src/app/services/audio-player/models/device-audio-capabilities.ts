export class DeviceAudioCapabilities {
  constructor(public deviceId: string,
              public capabilities: { [mimeType: string]: boolean }) {
  }
}
