export interface PagedDataItem<T> {
  copy: () => T;
}

export class PagedDataPage<T extends PagedDataItem<T>> {
  constructor(public pageIndex: number,
              public children: T[]) {
  }

  public copy(): PagedDataPage<T> {
    return new PagedDataPage<T>(this.pageIndex, this.children.map(child => child.copy()));
  }
}

export class PagedData<T extends PagedDataItem<T>> {
  public readonly pages: { [index: number]: PagedDataPage<T> } = {};

  public flatChildren: T[] = [];

  constructor(initialPages: PagedDataPage<T>[]) {
    this.pages = {};
    initialPages.forEach(page => {
      this.pages[page.pageIndex] = page;
    });

    this.flatChildren = PagedData.getFlatChildren(this.pages);
  }

  private static getFlatChildren<T extends PagedDataItem<T>>(pages: { [index: number]: PagedDataPage<T> }): T[] {
    return Object.values(pages).sort((a, b) => a.pageIndex - b.pageIndex).flatMap(page => page.children);
  }

  public addPage(pageIndex: number, children: T[]) {
    this.pages[pageIndex] = new PagedDataPage<T>(pageIndex, children);

    this.refreshFlatChildren();
  }

  public refreshFlatChildren(): void {
    this.flatChildren = PagedData.getFlatChildren(this.pages);
  }
}
