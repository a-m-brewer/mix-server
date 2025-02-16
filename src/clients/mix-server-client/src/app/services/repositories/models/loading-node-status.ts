export type LoadingAction =
  'RequestPlayback' |
  'Skip' |
  'Back' |
  'SaveTracklist' |
  'ImportTracklist' |
  'SetNextSession' |
  'ClearSession' |
  'GetQueue' |
  'LoadMoreHistoryItems' |
  'RequestPause' |
  'SyncPlaybackSession' |
  'PauseRequested';

export interface LoadingNodeStatus {
  loading: boolean;
  loadingIds: string[];

  isLoadingAction(action: LoadingAction): boolean;
}

export class LoadingNodeStatusImpl implements LoadingNodeStatus {
    constructor(public loading: boolean,
                public loadingIds: string[]) {
    }

    public isLoadingAction(action: LoadingAction): boolean {
        return this.loadingIds.includes(action);
    }

    public static get new(): LoadingNodeStatus {
        return new LoadingNodeStatusImpl(false, []);
    }
}
