export class EditQueueFormModel {
  constructor(public editing: boolean = false,
              public selectedItems: { [id: string]: boolean } = {}) {
  }

  public static copy(form: EditQueueFormModel): EditQueueFormModel {
    return new EditQueueFormModel(form.editing, form.selectedItems);
  }
}
