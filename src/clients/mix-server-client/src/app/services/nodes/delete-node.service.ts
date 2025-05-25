import { Injectable } from '@angular/core';
import {FileExplorerFileNode} from "../../main-content/file-explorer/models/file-explorer-file-node";
import {DeleteNodeCommand} from "../../generated-clients/mix-server-clients";
import {NodeManagementApiService} from "../api.service";
import {NodePathConverterService} from "../converters/node-path-converter.service";

@Injectable({
  providedIn: 'root'
})
export class DeleteNodeService {

  constructor(private _nodePathConverter: NodePathConverterService,
              private _nodeManagementClient: NodeManagementApiService) { }

  async delete(file: FileExplorerFileNode) {
    await this._nodeManagementClient.request(file.path.key,
      client => client.deleteNode(new DeleteNodeCommand({
        nodePath: this._nodePathConverter.toRequestDto(file.path)
      })), `Error deleting ${file.path.fileName}`);
  }
}
