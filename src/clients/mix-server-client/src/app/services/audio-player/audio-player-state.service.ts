import { Injectable } from '@angular/core';
import {BehaviorSubject, Observable} from "rxjs";
import {AudioPlayerState} from "./models/audio-player-state";
import {FileExplorerFileNode} from "../../main-content/file-explorer/models/file-explorer-file-node";

@Injectable({
  providedIn: 'root'
})
export class AudioPlayerStateService {
  private _stateBehaviourSubject$ = new BehaviorSubject<AudioPlayerState>(new AudioPlayerState());

  constructor() {
  }

  public get state$(): Observable<AudioPlayerState | null | undefined> {
    return this._stateBehaviourSubject$.asObservable();
  }

  public get state(): AudioPlayerState {
    return this._stateBehaviourSubject$.getValue();
  }

  public set node(node: FileExplorerFileNode | undefined | null) {
    const state = this.state;

    if (this.state.node?.isEqual(node)) {
      return;
    }

    const nextState = AudioPlayerState.copy(state);
    nextState.node = node;

    this.next(nextState);
  }

  public set queueItemId(queueItemId: string | null | undefined) {
    const state = this.state;

    if (state.queueItemId === queueItemId) {
      return;
    }

    const nextState = AudioPlayerState.copy(state);
    nextState.queueItemId = queueItemId;

    this.next(nextState);
  }

  public set playing(playing: boolean) {
    const state = this.state;

    if (state.playing === playing) {
      return;
    }

    const nextState = AudioPlayerState.copy(state);
    nextState.playing = playing;

    this.next(nextState);
  }

  public clear(): void {
    if (this.state.isDefault) {
      return;
    }

    this.next(new AudioPlayerState());
  }

  private next(state: AudioPlayerState): void {
    this._stateBehaviourSubject$.next(state);
  }
}
