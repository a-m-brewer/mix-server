import { Injectable } from '@angular/core';
import {FileExplorerFileNode} from "../../main-content/file-explorer/models/file-explorer-file-node";
import {FormBuilder, FormControl, FormGroup} from "@angular/forms";
import {BehaviorSubject, Subscription} from "rxjs";
import {CopyNodeCommand} from "../../generated-clients/mix-server-clients";
import {LoadingRepositoryService} from "../repositories/loading-repository.service";
import {ToastService} from "../toasts/toast-service";
import {NodeManagementApiService} from "../api.service";
import {NodePathConverterService} from "../converters/node-path-converter.service";

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
              private _nodePathConverter: NodePathConverterService,
              private _nodeManagementClient: NodeManagementApiService) {
    this._form = this.createForm();
  }

  public get isMove() {
    return this._form.value.move;
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

  async pasteNode(
    destinationNode: FileExplorerFileNode,
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

    await this._nodeManagementClient.request(sourceNode.path.key,
      client => client.copyNode(new CopyNodeCommand({
        sourcePath: this._nodePathConverter.toRequestDto(sourceNode.path),
        destinationPath: this._nodePathConverter.toRequestDto(destinationNode.path),
        overwrite,
        move: move ?? false
      })), 'Failed to copy node');

    this._form = this.createForm();
  }

  private createForm(): FormGroup<CopyNodeForm> {
    if (this._sourceControlSub) {
      this._sourceControlSub.unsubscribe();
      this._source$.next(null);
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

  resetForm() {
    this._form = this.createForm();
  }
}
