import type { CapacitorConfig } from '@capacitor/cli';

const androidScheme = process.env.CAPACITOR_ANDROID_SCHEME ?? 'https';
const allowMixedContent = process.env.CAPACITOR_ANDROID_ALLOW_MIXED_CONTENT === 'true';
const cleartext = process.env.CAPACITOR_SERVER_CLEARTEXT === 'true';

const config: CapacitorConfig = {
  appId: 'com.mixserver.app',
  appName: 'MixServer',
  webDir: 'dist/mix-server-client/browser',
  android: {
    allowMixedContent,
  },
  server: {
    androidScheme,
    cleartext,
    iosScheme: 'https',
  },
};

export default config;
