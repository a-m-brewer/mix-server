import {Component, OnDestroy, OnInit} from '@angular/core';
import {FormBuilder, FormControl, FormGroup, ReactiveFormsModule} from "@angular/forms";
import {FileExplorerFolderSortMode} from "../enums/file-explorer-folder-sort-mode";
import {FileExplorerNodeRepositoryService} from "../../../services/repositories/file-explorer-node-repository.service";
import {Subject, takeUntil} from "rxjs";
import {MatButtonToggleChange, MatButtonToggleModule} from "@angular/material/button-toggle";
import {LoadingRepositoryService} from "../../../services/repositories/loading-repository.service";
import {KeyValuePipe, NgForOf, NgIf} from "@angular/common";
import {MatIconModule} from "@angular/material/icon";

interface IFolderSortForm {
  descending: FormControl<boolean>;
  sortMode: FormControl<FileExplorerFolderSortMode>;
}

@Component({
  selector: 'app-folder-sort-form',
  standalone: true,
  templateUrl: './folder-sort-form.component.html',
  imports: [
    ReactiveFormsModule,
    MatButtonToggleModule,
    KeyValuePipe,
    MatIconModule,
    NgIf,
    NgForOf
  ],
  styleUrls: ['./folder-sort-form.component.scss']
})
export class FolderSortFormComponent implements OnInit, OnDestroy {
  private _unsubscribe$ = new Subject();
  private _lastSortMode: FileExplorerFolderSortMode = FileExplorerFolderSortMode.Name;

  public descendingKey = 'descending';
  public sortModeKey = 'sortMode';
  public sortModes = FileExplorerFolderSortMode;

  public disabled: boolean = false;
  public showForm: boolean = false;
  public form: FormGroup<IFolderSortForm>;

  constructor(private _formBuilder: FormBuilder,
              private _loadingRepository: LoadingRepositoryService,
              private _nodeRepository: FileExplorerNodeRepositoryService) {
    this.form = this._formBuilder.nonNullable.group<IFolderSortForm>({
      descending: _formBuilder.nonNullable.control<boolean>(false),
      sortMode: _formBuilder.nonNullable.control<FileExplorerFolderSortMode>(this._lastSortMode)
    });
  }

  public ngOnInit(): void {
    this._nodeRepository.currentFolder$
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(value => {
        this.showForm = !!value.node.absolutePath && value.node.absolutePath !== '';

        this.form.get(this.descendingKey)?.setValue(value.sort.descending);
        this.form.get(this.sortModeKey)?.setValue(value.sort.sortMode);

        this._lastSortMode = value.sort.sortMode;
      })

    this._loadingRepository.status$()
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(status => {
        this.disabled = status.loading;
      });
  }

  public ngOnDestroy(): void {
    this._unsubscribe$.next(null);
    this._unsubscribe$.complete();
  }

  public get sortMode(): FileExplorerFolderSortMode {
    return this.form.value.sortMode ?? FileExplorerFolderSortMode.Name;
  }

  public get descending(): boolean {
    return this.form.value.descending ?? false;
  }

  public onToggleButtonClicked(event: MatButtonToggleChange) {
    const nextSortMode = event.value as FileExplorerFolderSortMode;

    if (this._lastSortMode === nextSortMode) {
      const control = this.form.get(this.descendingKey);
      control?.setValue(!control?.value);
    }

    this.updateSortMode();

    this._lastSortMode = nextSortMode;
  }

  private updateSortMode(): void {
    this._nodeRepository.setFolderSort(this.form.value.sortMode ?? FileExplorerFolderSortMode.Name, this.form.value.descending ?? false);
  }
}
