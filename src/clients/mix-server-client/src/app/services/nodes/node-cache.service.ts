import {Injectable} from '@angular/core';
import {BehaviorSubject, combineLatestWith, firstValueFrom, map, Observable} from "rxjs";
import {FileExplorerFolder} from "../../main-content/file-explorer/models/file-explorer-folder";
import {LoadingRepositoryService} from "../repositories/loading-repository.service";
import {
  FolderSortMode,
  NodeClient,
  RefreshFolderCommand,
  SetFolderSortCommand
} from "../../generated-clients/mix-server-clients";
import {ToastService} from "../toasts/toast-service";
import {FileExplorerNodeConverterService} from "../converters/file-explorer-node-converter.service";
import {cloneDeep} from "lodash";
import {FolderSignalrClientService} from "../signalr/folder-signalr-client.service";
import {PlaybackDeviceService} from "../audio-player/playback-device.service";
import {Device} from "../repositories/models/device";
import {FileExplorerFileNode} from "../../main-content/file-explorer/models/file-explorer-file-node";
import {FileExplorerNode} from "../../main-content/file-explorer/models/file-explorer-node";
import {FileExplorerFolderNode} from "../../main-content/file-explorer/models/file-explorer-folder-node";
import {FileExplorerFolderSortMode} from "../../main-content/file-explorer/enums/file-explorer-folder-sort-mode";
import {PlaybackDeviceRepositoryService} from "../repositories/playback-device-repository.service";

@Injectable({
  providedIn: 'root'
})
export class NodeCacheService {
  private _folders$ = new BehaviorSubject<{ [absolutePath: string]: FileExplorerFolder }>({});

  constructor(private _fileExplorerNodeConverter: FileExplorerNodeConverterService,
              private _folderSignalRClient: FolderSignalrClientService,
              private _loadingRepository: LoadingRepositoryService,
              private _nodeClient: NodeClient,
              private _playbackDeviceService: PlaybackDeviceRepositoryService,
              private _toastService: ToastService) {
    this._playbackDeviceService.requestPlaybackDevice$
      .subscribe(device => {
        const folders = cloneDeep(this._folders$.value);
        this.updateFolders(folders, device);
      });

    this._folderSignalRClient.folderRefreshed$()
      .subscribe(updatedFolder => {
        this.updateFolder(updatedFolder);
      });

    this._folderSignalRClient.folderSorted$()
      .subscribe(updatedFolder => {
        this.updateFolder(updatedFolder);
        this._loadingRepository.stopLoading(updatedFolder.node.absolutePath);
      });

    this._folderSignalRClient.nodeUpdated$()
      .subscribe(event => {
        const node = event.node;

        const nextFolder = this.copyOrCreateParentFromNode(node);

        const oldPathIndex = !!event.oldAbsolutePath
          ? nextFolder.children.findIndex(n => n.absolutePath === event.oldAbsolutePath)
          : -1;

        const index = oldPathIndex === -1
          ? nextFolder.children.findIndex(n => n.absolutePath === node.absolutePath)
          : oldPathIndex;

        this.insertNodeIntoFolder(nextFolder, node, index, event.index);

        this.updateFolder(nextFolder);
      });

    this._folderSignalRClient.nodeDeleted$()
      .subscribe(event => {
        const nextFolder = this.copyOrCreateParent(event.parent);

        nextFolder.children = nextFolder.children.filter(f => f.absolutePath !== event.absolutePath);

        this.updateFolder(nextFolder);
      });
  }

  public getFolder$(query$: Observable<string>): Observable<FileExplorerFolder> {
    return this._folders$
      .pipe(combineLatestWith(query$))
      .pipe(map(([folders, absolutePath]) => folders[absolutePath] ?? FileExplorerFolder.Default));
  }

  getFileByNode$(initialNode: FileExplorerFileNode): Observable<FileExplorerFileNode> {
    const existingFolder = this._folders$.value[initialNode.parent.absolutePath];

    const nodeIndex = !!existingFolder
      ? existingFolder.children.findIndex(n => n.absolutePath === initialNode.absolutePath)
      : -1;

    if (existingFolder && nodeIndex !== -1) {
      const nextFolder = existingFolder.copy();
      nextFolder.children.splice(nodeIndex, 1, initialNode);
    }

    return this._folders$
      .pipe(map(folders => {
        const folder = folders[initialNode.parent.absolutePath];
        const node = folder?.children.find(n => n.absolutePath === initialNode.absolutePath);

        if (!node || !(node instanceof FileExplorerFileNode)) {
          return initialNode;
        }

        return node;
      }));
  }

  public async loadDirectory(absolutePath: string): Promise<string> {
    const loadingKey = absolutePath === "" ? "root" : absolutePath;

    this._loadingRepository.startLoading(loadingKey);

    try {
      const folderResponse = await firstValueFrom(this._nodeClient.getNode(absolutePath))
      const folder = this._fileExplorerNodeConverter.fromFileExplorerFolder(folderResponse);
      this.updateFolder(folder);

      return folder.node.absolutePath;
    } catch (e) {
      this._toastService.logServerError(e);

      return "";
    } finally {
      this._loadingRepository.stopLoading(loadingKey);
    }
  }

  public async refreshFolder(absolutePath: string): Promise<void> {
    this._loadingRepository.startLoading(absolutePath);

    try {
      const response = await firstValueFrom(this._nodeClient.refreshFolder(new RefreshFolderCommand({
        absolutePath: absolutePath
      })));

      const folder = this._fileExplorerNodeConverter.fromFileExplorerFolder(response);

      this.updateFolder(folder);
    } catch (e) {
      this._toastService.logServerError(e, 'Failed to refresh folder');
    } finally {
      this._loadingRepository.stopLoading(absolutePath);
    }
  }

  public async setFolderSort(absolutePath: string, sortMode: FileExplorerFolderSortMode, descending: boolean): Promise<void> {
    this._loadingRepository.startLoading(absolutePath)

    try {
      this._nodeClient.setFolderSortMode(new SetFolderSortCommand({
        absoluteFolderPath: absolutePath,
        sortMode: this.toFolderSortMode(sortMode),
        descending: descending
      }));
    } catch (e) {
      this._toastService.logServerError(e, 'Failed to update folder sort');
    } finally {
      this._loadingRepository.stopLoading(absolutePath);
    }
  }

  private copyOrCreateParentFromNode(node: FileExplorerNode) {
    return this.copyOrCreateParent(node.parent);
  }

  private copyOrCreateParent(parent: FileExplorerFolderNode | null | undefined): FileExplorerFolder {
    const existingFolder = this._folders$.value[parent?.absolutePath ?? ""]
    return existingFolder
      ? existingFolder.copy()
      : FileExplorerFolder.Default;
  }

  private updateFolder(folder: FileExplorerFolder): void {
    this.updateFolders({[folder.node.absolutePath]: folder});
  }

  private updateFolders(updates: { [absolutePath: string]: FileExplorerFolder }, device?: Device | null): void {
    const requestedPlaybackDevice = device ?? this._playbackDeviceService.requestPlaybackDevice;
    Object.values(updates).forEach(folder => {
      folder.children.forEach(node => {
        if (node instanceof FileExplorerFileNode) {
          node.updateCanPlay(requestedPlaybackDevice);
        }
      })
    });

    const existingFolders = cloneDeep(this._folders$.value);
    const nextFolders = {...existingFolders, ...updates};
    this._folders$.next(nextFolders);
  }

  private insertNodeIntoFolder(folder: FileExplorerFolder, node: FileExplorerNode, index: number, eventIndex: number): void {
    if (index === -1) {
      if (eventIndex !== -1 && eventIndex < folder.children.length) {
        folder.children.splice(eventIndex, 0, node);
      }
      else {
        folder.children.push(node);
      }
    }
    else {
      folder.children[index] = node;
    }
  }

  private toFolderSortMode(sortMode: FileExplorerFolderSortMode): FolderSortMode {
    switch (sortMode) {
      case FileExplorerFolderSortMode.Created:
        return FolderSortMode.Created;
      case FileExplorerFolderSortMode.Name:
        return FolderSortMode.Name;
      default:
        return FolderSortMode.Name;
    }
  }
}
