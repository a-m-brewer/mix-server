import { registerPlugin } from '@capacitor/core';
import type { NativeAudioPlugin } from './definitions';

const NativeAudio = registerPlugin<NativeAudioPlugin>('NativeAudio');

export * from './definitions';
export { NativeAudio };
