import { Injectable } from '@angular/core';
import {FileExplorerFileNode} from "../../main-content/file-explorer/models/file-explorer-file-node";
import {DeleteNodeCommand} from "../../generated-clients/mix-server-clients";
import {NodeManagementApiService} from "../api.service";

@Injectable({
  providedIn: 'root'
})
export class DeleteNodeService {

  constructor(private _nodeManagementClient: NodeManagementApiService) { }

  async delete(file: FileExplorerFileNode) {
    await this._nodeManagementClient.request(file.absolutePath,
      client => client.deleteNode(new DeleteNodeCommand({
        absolutePath: file.absolutePath
      })), `Error deleting ${file.name}`);
  }
}
