import {Injectable} from '@angular/core';
import {BehaviorSubject, combineLatestWith, firstValueFrom, map, Observable} from "rxjs";
import {FileExplorerFolder} from "../../main-content/file-explorer/models/file-explorer-folder";
import {LoadingRepositoryService} from "../repositories/loading-repository.service";
import {
  FolderSortMode,
  RefreshFolderCommand,
  SetFolderSortCommand
} from "../../generated-clients/mix-server-clients";
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
import {NodeApiService} from "../api.service";
import {NodePath, NodePathHeader} from "../../main-content/file-explorer/models/node-path";
import {NodePathConverterService} from "../converters/node-path-converter.service";

@Injectable({
  providedIn: 'root'
})
export class NodeCacheService {
  private _folders$ = new BehaviorSubject<{ [nodeKey: string]: FileExplorerFolder }>({});

  constructor(private _fileExplorerNodeConverter: FileExplorerNodeConverterService,
              private _folderSignalRClient: FolderSignalrClientService,
              private _loadingRepository: LoadingRepositoryService,
              private _nodeClient: NodeApiService,
              private _nodePathConverter: NodePathConverterService,
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
        this._loadingRepository.stopLoading(updatedFolder.node.path.key);
      });

    this._folderSignalRClient.nodeUpdated$()
      .subscribe(event => {
        const node = event.node;

        const nextFolder = this.copyOrCreateParentFromNode(node);

        const oldPathIndex = !!event.oldPath
          ? nextFolder.children.findIndex(n => n.path.isEqual(event.oldPath))
          : -1;

        const index = oldPathIndex === -1
          ? nextFolder.children.findIndex(n => n.path.isEqual(node.path))
          : oldPathIndex;

        this.insertNodeIntoFolder(nextFolder, node, index, event.index);

        this.updateFolder(nextFolder);
      });

    this._folderSignalRClient.nodeDeleted$()
      .subscribe(event => {
        const nextFolder = this.copyOrCreateParent(event.parent);

        nextFolder.children = nextFolder.children.filter(f => !f.path.isEqual(event.nodePath));

        this.updateFolder(nextFolder);
      });

    this._folderSignalRClient.mediaInfoUpdated$()
      .subscribe(event => {
        const groupedUpdates = event.mediaInfo
          .reduce((acc, item) => {
            if (!(item.nodePath.parent.key in acc)) {
              acc[item.nodePath.parent.key] = [];
            }

            acc[item.nodePath.parent.key].push(item);

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
            const node = nextFolder.children.find(n => n.path.fileName === f.nodePath.fileName);
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
            if (!(item.parent.key in acc)) {
              acc[item.parent.key] = [];
            }

            acc[item.parent.key].push(item);

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
            const node = nextFolder.children.find(n => n.path.fileName === f.fileName);
            if (node instanceof FileExplorerFileNode) {
              node.metadata.mediaInfo = null;
            }
          });

          updatedFolders[absolutePath] = nextFolder;
        });

        this.updateFolders(updatedFolders);
      });
  }

  public getFolder$(query$: Observable<NodePathHeader>): Observable<FileExplorerFolder> {
    return this._folders$
      .pipe(combineLatestWith(query$))
      .pipe(map(([folders, header]) => folders[header.key] ?? FileExplorerFolder.Default));
  }

  getFileByNode$(initialNode: FileExplorerFileNode): Observable<FileExplorerFileNode> {
    const existingFolder = this._folders$.value[initialNode.parent.path.key];

    const nodeIndex = !!existingFolder
      ? existingFolder.children.findIndex(n => n.path.isEqual(initialNode.path))
      : -1;

    if (existingFolder && nodeIndex !== -1) {
      const nextFolder = existingFolder.copy();
      nextFolder.children.splice(nodeIndex, 1, initialNode);
    }

    return this._folders$
      .pipe(map(folders => {
        const folder = folders[initialNode.parent.path.key];
        const node = folder?.children.find(n => n.path.isEqual(initialNode.path));

        if (!node || !(node instanceof FileExplorerFileNode)) {
          return initialNode;
        }

        return node;
      }));
  }

  public async loadDirectory(path: NodePathHeader): Promise<NodePathHeader> {
    if (this._folders$.value[path.key]) {
      return path;
    }

    const loadingKey = path.key === "" ? "root" : path.key;

    if (this._loadingRepository.isLoading(loadingKey)) {
      return path;
    }

    const result = await this._nodeClient.request(loadingKey,
      client => client.getNode(path.rootPath, path.relativePath), 'Error loading directory');
    console.log('got node', result);

    if (result.result) {
      const folder = this._fileExplorerNodeConverter.fromFileExplorerFolder(result.result);

      this.updateFolder(folder);

      return folder.node.path;
    }

    return new NodePathHeader("", "");
  }

  public refreshFolder(nodePath: NodePathHeader): void {
    this._nodeClient.request(nodePath.key,
      client => client.refreshFolder(new RefreshFolderCommand({
        nodePath: this._nodePathConverter.toRequestDto(nodePath)
      })), 'Failed to refresh folder')
      .then(result => result.success(dto => {
        const folder = this._fileExplorerNodeConverter.fromFileExplorerFolder(dto);
        this.updateFolder(folder);
      }))
  }

  public async setFolderSort(nodePath: NodePathHeader, sortMode: FileExplorerFolderSortMode, descending: boolean): Promise<void> {
    await this._nodeClient.request(nodePath.key,
      client => client.setFolderSortMode(new SetFolderSortCommand({
        nodePath: this._nodePathConverter.toRequestDto(nodePath),
        sortMode: this.toFolderSortMode(sortMode),
        descending: descending
      })), 'Failed to update folder sort');
  }

  private copyOrCreateParentFromNode(node: FileExplorerNode) {
    return this.copyOrCreateParent(node.parent);
  }

  private copyOrCreateParent(parent: FileExplorerFolderNode | null | undefined): FileExplorerFolder {
    const existingFolder = this._folders$.value[parent?.path.key ?? ""]
    return existingFolder
      ? existingFolder.copy()
      : FileExplorerFolder.Default;
  }

  private updateFolder(folder: FileExplorerFolder): void {
    this.updateFolders({[folder.node.path.key]: folder});
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
