import { Injectable } from '@angular/core';
import {FileExplorerFileNode} from "../../main-content/file-explorer/models/file-explorer-file-node";
import {RequestTranscodeCommand} from "../../generated-clients/mix-server-clients";
import {TranscodeApiService} from "../api.service";
import {NodePathConverterService} from "../converters/node-path-converter.service";

@Injectable({
  providedIn: 'root'
})
export class TranscodeService {

  constructor(private _nodePathConverter: NodePathConverterService,
              private _transcodeClient: TranscodeApiService) { }

  public async requestTranscode(file: FileExplorerFileNode): Promise<void> {
    await this._transcodeClient.request(file.path.key,
        client => client.requestTranscode(new RequestTranscodeCommand({
      nodePath: this._nodePathConverter.toRequestDto(file.path)
    })), 'Failed to request transcode file');
  }
}
