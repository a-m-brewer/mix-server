export class FileMetadata {
  constructor(public mimeType: string) {
  }

  copy() {
    return new FileMetadata(this.mimeType);
  }
}
