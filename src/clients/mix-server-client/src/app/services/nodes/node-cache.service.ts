import {Injectable} from '@angular/core';
import {BehaviorSubject, combineLatestWith, firstValueFrom, map, Observable} from "rxjs";
import {FileExplorerFolder} from "../../main-content/file-explorer/models/file-explorer-folder";
import {LoadingRepositoryService} from "../repositories/loading-repository.service";
import {
  FolderSortMode,
  RefreshFolderCommand,
  SetFolderSortCommand
} from "../../generated-clients/mix-server-clients";
import {ToastService} from "../toasts/toast-service";
import {FileExplorerNodeConverterService} from "../converters/file-explorer-node-converter.service";
import {cloneDeep} from "lodash";
import {FolderSignalrClientService} from "../signalr/folder-signalr-client.service";
import {Device} from "../repositories/models/device";
import {FileExplorerFileNode} from "../../main-content/file-explorer/models/file-explorer-file-node";
import {FileExplorerNode} from "../../main-content/file-explorer/models/file-explorer-node";
import {FileExplorerFolderNode} from "../../main-content/file-explorer/models/file-explorer-folder-node";
import {FileExplorerFolderSortMode} from "../../main-content/file-explorer/enums/file-explorer-folder-sort-mode";
import {PlaybackDeviceRepositoryService} from "../repositories/playback-device-repository.service";
import {MediaInfoUpdatedEventItem} from "../signalr/models/media-info-event";
import {NodePath} from "../repositories/models/node-path";
import {NodeApiService} from "../api.service";

@Injectable({
  providedIn: 'root'
})
export class NodeCacheService {
  private _folders$ = new BehaviorSubject<{ [absolutePath: string]: FileExplorerFolder }>({});

  constructor(private _fileExplorerNodeConverter: FileExplorerNodeConverterService,
              private _folderSignalRClient: FolderSignalrClientService,
              private _loadingRepository: LoadingRepositoryService,
              private _nodeClient: NodeApiService,
              private _playbackDeviceService: PlaybackDeviceRepositoryService) {
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

    this._folderSignalRClient.mediaInfoUpdated$()
      .subscribe(event => {
        const groupedUpdates = event.mediaInfo
          .reduce((acc, item) => {
            if (!(item.nodePath.parentAbsolutePath in acc)) {
              acc[item.nodePath.parentAbsolutePath] = [];
            }

            acc[item.nodePath.parentAbsolutePath].push(item);

            return acc;
          }, {} as { [absolutePath: string]: MediaInfoUpdatedEventItem[] });

        const updatedFolders: { [absolutePath: string]: FileExplorerFolder } = {};

        Object.entries(groupedUpdates).forEach(([absolutePath, mediaInfos]) => {
          const parent = this._folders$.value[absolutePath];
          if (!parent) {
            return;
          }

          const nextFolder = parent.copy();
          mediaInfos.forEach(f => {
            const node = nextFolder.children.find(n => n.name === f.nodePath.fileName);
            if (node instanceof FileExplorerFileNode) {
              node.metadata.mediaInfo = f.info;
            }
          });

          updatedFolders[absolutePath] = nextFolder;
        });

        this.updateFolders(updatedFolders);
      });

    this._folderSignalRClient.mediaInfoRemoved$()
      .subscribe(event => {
        const groupedUpdates = event.nodePaths
          .reduce((acc, item) => {
            if (!(item.parentAbsolutePath in acc)) {
              acc[item.parentAbsolutePath] = [];
            }

            acc[item.parentAbsolutePath].push(item);

            return acc;
          }, {} as { [absolutePath: string]: NodePath[] });

        const updatedFolders: { [absolutePath: string]: FileExplorerFolder } = {};

        Object.entries(groupedUpdates).forEach(([absolutePath, mediaInfos]) => {
          const parent = this._folders$.value[absolutePath];
          if (!parent) {
            return;
          }

          const nextFolder = parent.copy();
          mediaInfos.forEach(f => {
            const node = nextFolder.children.find(n => n.name === f.fileName);
            if (node instanceof FileExplorerFileNode) {
              node.metadata.mediaInfo = null;
            }
          });

          updatedFolders[absolutePath] = nextFolder;
        });

        this.updateFolders(updatedFolders);
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
    if (this._folders$.value[absolutePath]) {
      return absolutePath;
    }

    const loadingKey = absolutePath === "" ? "root" : absolutePath;

    if (this._loadingRepository.isLoading(loadingKey)) {
      return absolutePath;
    }

    const result = await this._nodeClient.request(loadingKey,
      client => client.getNode(absolutePath), 'Error loading directory');

    if (result.result) {
      const folder = this._fileExplorerNodeConverter.fromFileExplorerFolder(result.result);

      this.updateFolder(folder);

      return folder.node.absolutePath;
    }

    return "";
  }

  public refreshFolder(absolutePath: string): void {
    this._nodeClient.request(absolutePath,
      client => client.refreshFolder(new RefreshFolderCommand({
        absolutePath: absolutePath
      })), 'Failed to refresh folder')
      .then(result => result.success(dto => {
        const folder = this._fileExplorerNodeConverter.fromFileExplorerFolder(dto);
        this.updateFolder(folder);
      }))
  }

  public async setFolderSort(absolutePath: string, sortMode: FileExplorerFolderSortMode, descending: boolean): Promise<void> {
    await this._nodeClient.request(absolutePath,
      client => client.setFolderSortMode(new SetFolderSortCommand({
        absoluteFolderPath: absolutePath,
        sortMode: this.toFolderSortMode(sortMode),
        descending: descending
      })), 'Failed to update folder sort');
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
