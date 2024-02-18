import { Injectable } from '@angular/core';
import {BehaviorSubject, Observable} from "rxjs";
import {AudioPlayerStateModel} from "./models/audio-player-state-model";
import {FileExplorerFileNode} from "../../main-content/file-explorer/models/file-explorer-file-node";

@Injectable({
  providedIn: 'root'
})
export class AudioPlayerStateService {
  private _stateBehaviourSubject$ = new BehaviorSubject<AudioPlayerStateModel>(new AudioPlayerStateModel());

  constructor() {
  }

  public get state$(): Observable<AudioPlayerStateModel> {
    return this._stateBehaviourSubject$.asObservable();
  }

  public get state(): AudioPlayerStateModel {
    return this._stateBehaviourSubject$.getValue();
  }

  public set node(node: FileExplorerFileNode | undefined | null) {
    const state = this.state;

    if (this.state.node?.isEqual(node)) {
      return;
    }

    const nextState = AudioPlayerStateModel.copy(state);
    nextState.node = node;

    this.next(nextState);
  }

  public set queueItemId(queueItemId: string | null | undefined) {
    const state = this.state;

    if (state.queueItemId === queueItemId) {
      return;
    }

    const nextState = AudioPlayerStateModel.copy(state);
    nextState.queueItemId = queueItemId;

    this.next(nextState);
  }

  public set playing(playing: boolean) {
    const state = this.state;

    if (state.playing === playing) {
      return;
    }

    const nextState = AudioPlayerStateModel.copy(state);
    nextState.playing = playing;

    this.next(nextState);
  }

  public clear(): void {
    if (this.state.isDefault) {
      return;
    }

    this.next(new AudioPlayerStateModel());
  }

  private next(state: AudioPlayerStateModel): void {
    this._stateBehaviourSubject$.next(state);
  }
}
