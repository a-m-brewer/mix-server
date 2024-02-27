import {Component, OnDestroy, OnInit} from '@angular/core';
import {FormBuilder, FormControl, FormGroup, ReactiveFormsModule} from "@angular/forms";
import {Subject, takeUntil} from "rxjs";
import {QueueRepositoryService} from "../../services/repositories/queue-repository.service";
import {EditQueueFormModel} from "../../services/repositories/models/edit-queue-form-model";
import {QueueEditFormRepositoryService} from "../../services/repositories/queue-edit-form-repository.service";
import {MatButtonModule} from "@angular/material/button";
import {NgIf} from "@angular/common";

interface IQueueEditForm {
  editMode: FormControl<boolean>;
}

@Component({
  selector: 'app-queue-edit-form',
  standalone: true,
  templateUrl: './queue-edit-form.component.html',
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    NgIf
  ],
  styleUrls: ['./queue-edit-form.component.scss']
})
export class QueueEditFormComponent implements OnInit, OnDestroy {
  private _unsubscribe$ = new Subject();
  public editModeKey = 'editMode';

  public form: FormGroup<IQueueEditForm>;

  public model: EditQueueFormModel = new EditQueueFormModel();

  public hasSelectedItems = false;

  constructor(_formBuilder: FormBuilder,
              private _queueRepository: QueueRepositoryService,
              private _queueEditFormRepository: QueueEditFormRepositoryService) {
    this.form = _formBuilder.nonNullable.group<IQueueEditForm>({
      editMode: _formBuilder.nonNullable.control<boolean>(false)
    });
  }

  public ngOnInit(): void {
    this.form?.valueChanges
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(form => {
        this._queueEditFormRepository.updateEditForm(f => {
          f.editing = form.editMode ?? false;
          if (!f.editing) {
            f.selectedItems = {};
          }
        });
      });

    this._queueEditFormRepository.editForm$
      .pipe(takeUntil(this._unsubscribe$))
      .subscribe(model => {
        this.model = model;
        this.hasSelectedItems = Object.entries(model.selectedItems).some(([_, v]) => v);
      });
  }

  public ngOnDestroy(): void {
    this._unsubscribe$.next(null);
    this._unsubscribe$.complete();
  }

  public onEditModeChanged(): void {
    const editModeControl = this.form.get(this.editModeKey);
    const editModeValue = editModeControl?.value ?? false;
    editModeControl?.setValue(!editModeValue);
  }

  public onRemoveQueueItems(): void {
    if (!this.hasSelectedItems) {
      return;
    }

    const selectedItems = Object
      .entries(this.model.selectedItems)
      .filter(([, v]) => v)
      .map(([k, ]) => k);

    if (selectedItems.length === 0) {
      return;
    }

    this._queueRepository.removeRangeFromQueue(selectedItems);

    this.onEditModeChanged();
  }
}
