import { Injectable } from '@angular/core';
import {FileExplorerFileNode} from "../../main-content/file-explorer/models/file-explorer-file-node";
import {FormBuilder, FormControl, FormGroup} from "@angular/forms";
import {BehaviorSubject, firstValueFrom, Subscription} from "rxjs";
import {CopyNodeCommand, NodeManagementClient} from "../../generated-clients/mix-server-clients";
import {LoadingRepositoryService} from "../repositories/loading-repository.service";
import {ToastService} from "../toasts/toast-service";

interface CopyNodeForm {
  sourceNode: FormControl<FileExplorerFileNode | null>;
  move: FormControl<boolean>;
  overwrite: FormControl<boolean>;
  destinationNode: FormControl<FileExplorerFileNode | null>;
}

@Injectable({
  providedIn: 'root'
})
export class CopyNodeService {
  private _sourceControlSub: Subscription | null = null;

  private _source$ = new BehaviorSubject<FileExplorerFileNode | null>(null);
  private _form: FormGroup<CopyNodeForm>;

  constructor(private _formBuilder: FormBuilder,
              private _loading: LoadingRepositoryService,
              private _toast: ToastService,
              private _nodeManagementClient: NodeManagementClient) {
    this._form = this.createForm();
  }

  public get sourceNode$() {
    return this._source$.asObservable();
  }

  setSourceNode(sourceNode: FileExplorerFileNode,
                move: boolean) {
    this._form.patchValue({
      sourceNode,
      move
    });
  }

  async pasteNode(destinationNode: FileExplorerFileNode,
            overwrite: boolean) {
    this._form.patchValue({
      destinationNode,
      overwrite
    });

    const {
      sourceNode,
      move
    } = this._form.value;

    if (!sourceNode) {
      return;
    }

    this._loading.startLoadingId(sourceNode.absolutePath)
    try {
      await firstValueFrom(this._nodeManagementClient.copyNode(new CopyNodeCommand({
        sourceAbsolutePath: sourceNode.absolutePath,
        destinationFolder: destinationNode.parent.absolutePath,
        destinationName: destinationNode.name,
        overwrite,
        move: move ?? false
      })));
    } catch (err) {
      this._toast.logServerError(err, 'Failed to copy node');
    } finally {
      this._loading.stopLoadingId(sourceNode.absolutePath);
      this._form = this.createForm();
    }
  }

  private createForm(): FormGroup<CopyNodeForm> {
    if (this._sourceControlSub) {
      this._sourceControlSub.unsubscribe();
    }

    const form = this._formBuilder.group<CopyNodeForm>({
      sourceNode: this._formBuilder.control<FileExplorerFileNode | null>(null),
      move: this._formBuilder.nonNullable.control<boolean>(false),
      overwrite: this._formBuilder.nonNullable.control<boolean>(false),
      destinationNode: this._formBuilder.control<FileExplorerFileNode | null>(null)
    })

    this._sourceControlSub = form.controls.sourceNode.valueChanges.subscribe((sourceNode) => {
      this._source$.next(sourceNode);
    });

    return form;
  }
}
