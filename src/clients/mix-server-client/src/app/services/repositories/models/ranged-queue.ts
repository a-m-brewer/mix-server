import {QueueItem} from "./queue-item";

export class RangedQueue {
  private _items: { [id: string]: QueueItem } = {};

  public items: QueueItem[] = [];

  constructor(initialItems: QueueItem[]) {
    this._items = {};
    this.addRange(initialItems);
  }

  public static get Default(): RangedQueue {
    return new RangedQueue([]);
  }

  public copy(): RangedQueue {
    return new RangedQueue(
      Object.values(this.items).map(item => item.copy())
    );
  }

  public addRange(range: QueueItem[]): void {
    range.forEach(item => {
      this._items[item.id] = item;
    });

    this.refreshItems();
  }

  public refreshItems(): void {
    this.items = Object.values(this._items)
      .sort((a, b) => {
        if (a.rank < b.rank) return -1;
        if (a.rank > b.rank) return 1;
        return 0;
      });
  }

  removeRange(removed: string[]) {
    removed.forEach(id => {
      delete this._items[id];
    });

    this.refreshItems();
  }
}
