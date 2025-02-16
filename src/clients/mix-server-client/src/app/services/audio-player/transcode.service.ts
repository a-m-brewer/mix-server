import { Injectable } from '@angular/core';
import {FileExplorerFileNode} from "../../main-content/file-explorer/models/file-explorer-file-node";
import {RequestTranscodeCommand, TranscodeClient} from "../../generated-clients/mix-server-clients";
import {ToastService} from "../toasts/toast-service";
import {LoadingRepositoryService} from "../repositories/loading-repository.service";
import {firstValueFrom} from "rxjs";

@Injectable({
  providedIn: 'root'
})
export class TranscodeService {

  constructor(private _loading: LoadingRepositoryService,
              private _toastService: ToastService,
              private _transcodeClient: TranscodeClient) { }

  public async requestTranscode(file: FileExplorerFileNode): Promise<void> {
    this._loading.startLoadingId(file.absolutePath);

    try {
      await firstValueFrom(this._transcodeClient.requestTranscode(new RequestTranscodeCommand({
        absoluteFilePath: file.absolutePath
      })))
    } catch (err) {
      this._toastService.logServerError(err, 'Failed to request transcode file');
    } finally {
      this._loading.stopLoadingId(file.absolutePath);
    }
  }
}
