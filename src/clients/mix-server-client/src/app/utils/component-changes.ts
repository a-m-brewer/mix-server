export type ComponentChange<T, P extends  keyof T> = {
  previousValue: T[P];
  currentValue: T[P];
  firstChange: boolean;
}

export type ComponentChanges<T> = {
  [P in keyof T]?: ComponentChange<T, P>;
};
