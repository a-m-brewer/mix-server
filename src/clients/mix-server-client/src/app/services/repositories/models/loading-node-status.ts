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
  loadingIds: { [key: string]: number };

  isLoadingAction(action: LoadingAction): boolean;
}

export class LoadingNodeStatusImpl implements LoadingNodeStatus {
    constructor(public loading: boolean,
                public loadingIds: { [key: string]: number }) {
    }

    public isLoadingAction(action: LoadingAction): boolean {
        return this.loadingIds[action] > 0;
    }

    public static get new(): LoadingNodeStatus {
        return new LoadingNodeStatusImpl(false, {});
    }
}
