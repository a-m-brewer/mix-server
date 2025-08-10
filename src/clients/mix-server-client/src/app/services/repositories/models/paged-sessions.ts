import {PagedData} from "../../data-sources/paged-data";
import {PlaybackSession} from "./playback-session";

export class PagedSessions extends PagedData<PlaybackSession> {
  public static get Default(): PagedSessions {
    return new PagedSessions([]);
  }

  public copy(): PagedSessions {
    return new PagedSessions(Object.values(this.pages).map(page => page.copy()));
  }
}
