export enum MenuLabel {
  Queue = 'Queue',
  History = 'History',
  Logout = 'Logout',
  Admin = 'Admin',
  Files = 'Files',
}

export interface MenuItem {
  color?: string | null;
  label: MenuLabel;
  icon: string;
  show: boolean;
  showOnMobile: boolean;
  showOnTablet: boolean;
  showOnDesktop: boolean;
  route: string | (() => void);
}
