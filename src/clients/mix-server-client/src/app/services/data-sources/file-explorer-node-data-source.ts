import {CollectionViewer, DataSource} from "@angular/cdk/collections";
import {FileExplorerNode} from "../../main-content/file-explorer/models/file-explorer-node";
import {map, Observable, Subscription} from "rxjs";
import {FileExplorerNodeRepositoryService} from "../repositories/file-explorer-node-repository.service";

export class FileExplorerNodeDataSource extends DataSource<FileExplorerNode> {
  private readonly _subscription = new Subscription();

  public pageSize: number = 25;

  constructor(private _nodeRepository: FileExplorerNodeRepositoryService) {
    super();
  }

  connect(collectionViewer: CollectionViewer): Observable<FileExplorerNode[]> {
    this.pageSize = this._nodeRepository.pageSize;

    this._subscription.add(
      collectionViewer.viewChange.subscribe(range => {
        const pageStart = Math.floor(range.start / this.pageSize);
        const pageEnd = Math.floor(range.end / this.pageSize);

        for (let i = pageStart; i <= pageEnd; i++) {
          this._nodeRepository.loadPage(i)
            .then();
        }
      })
    )

    return this._nodeRepository.currentFolder$
      .pipe(map(currentFolder => {
        return currentFolder.flatChildren;
      }));
  }

  disconnect(collectionViewer: CollectionViewer): void {
    this._subscription.unsubscribe();
  }

}
