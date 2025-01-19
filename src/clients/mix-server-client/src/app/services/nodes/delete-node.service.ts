import { Injectable } from '@angular/core';
import {FileExplorerFileNode} from "../../main-content/file-explorer/models/file-explorer-file-node";
import {DeleteNodeCommand, NodeManagementClient} from "../../generated-clients/mix-server-clients";
import {ToastService} from "../toasts/toast-service";
import {LoadingRepositoryService} from "../repositories/loading-repository.service";
import {firstValueFrom} from "rxjs";

@Injectable({
  providedIn: 'root'
})
export class DeleteNodeService {

  constructor(private _nodeManagementClient: NodeManagementClient,
              private _toast: ToastService,
              private _loading: LoadingRepositoryService) { }

  async delete(file: FileExplorerFileNode) {
    this._loading.startLoadingId(file.absolutePath);

    try {
      await firstValueFrom(this._nodeManagementClient.deleteNode(new DeleteNodeCommand({
        absolutePath: file.absolutePath
      })));
    } catch (e) {
      this._toast.logServerError(e, 'Failed to delete node');
    } finally {
      this._loading.stopLoadingId(file.absolutePath);
    }
  }
}
