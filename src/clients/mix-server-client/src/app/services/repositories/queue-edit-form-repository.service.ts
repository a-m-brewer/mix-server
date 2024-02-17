import { Injectable } from '@angular/core';
import {BehaviorSubject, Observable} from "rxjs";
import {EditQueueFormModel} from "./models/edit-queue-form-model";

@Injectable({
  providedIn: 'root'
})
export class QueueEditFormRepositoryService {
  private _editQueueFormBehaviourSubject$ = new BehaviorSubject<EditQueueFormModel>(new EditQueueFormModel());

  constructor() { }

  public get editForm$(): Observable<EditQueueFormModel> {
    return this._editQueueFormBehaviourSubject$.asObservable();
  }

  public get editForm(): EditQueueFormModel {
    return this._editQueueFormBehaviourSubject$.getValue();
  }

  public updateEditForm(update: (form: EditQueueFormModel) => void): void {
    const form = EditQueueFormModel.copy(this.editForm);

    update(form);

    this._editQueueFormBehaviourSubject$.next(form);
  }
}
