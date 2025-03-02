import { Injectable } from '@angular/core';
import {FileExplorerFileNode} from "../../main-content/file-explorer/models/file-explorer-file-node";
import {RequestTranscodeCommand, TranscodeClient} from "../../generated-clients/mix-server-clients";
import {ToastService} from "../toasts/toast-service";
import {LoadingRepositoryService} from "../repositories/loading-repository.service";
import {firstValueFrom} from "rxjs";
import {TranscodeApiService} from "../api.service";

@Injectable({
  providedIn: 'root'
})
export class TranscodeService {

  constructor(private _transcodeClient: TranscodeApiService) { }

  public async requestTranscode(file: FileExplorerFileNode): Promise<void> {
    await this._transcodeClient.request(file.absolutePath, client => client.requestTranscode(new RequestTranscodeCommand({
      absoluteFilePath: file.absolutePath
    })), 'Failed to request transcode file');
  }
}
