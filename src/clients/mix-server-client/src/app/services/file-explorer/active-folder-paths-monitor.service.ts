import { Injectable } from '@angular/core';
import {FileExplorerNodeRepositoryService} from "../repositories/file-explorer-node-repository.service";
import {FileExplorerNodeStateRepositoryService} from "../repositories/file-explorer-node-state-repository.service";
import {QueueRepositoryService} from "../repositories/queue-repository.service";
import {HistoryRepositoryService} from "../repositories/history-repository.service";

@Injectable({
  providedIn: 'root'
})
export class ActiveFolderPathsMonitorService {

  constructor(private _fileExplorerNodeRepository: FileExplorerNodeRepositoryService,
              private _historyRepository: HistoryRepositoryService,
              private _fileExplorerNodeStateRepository: FileExplorerNodeStateRepositoryService,
              private _queueRepository: QueueRepositoryService)
  {
  }

  public initialize() {
    this._fileExplorerNodeRepository.currentFolder$
      .subscribe(value => {
        const inUseFileExplorerFiles = value.children.map(n => n.absolutePath);
        this._fileExplorerNodeStateRepository.setInUseFileExplorerPaths(inUseFileExplorerFiles);
      });

    this._queueRepository.queue$()
      .subscribe(value => {
        const inUserQueueFiles = value.items.map(i => i.file.absolutePath);
        this._fileExplorerNodeStateRepository.setInUseQueuePaths(inUserQueueFiles);
      });

    this._historyRepository.sessions$
      .subscribe(value => {
        const files = value.map(s => s.currentNode.absolutePath);
        this._fileExplorerNodeStateRepository.setInUseHistoryPaths(files);
      });
  }
}
