import {environment} from "../environments/environment";
import {EnvironmentType} from "../environments/environment-type.enum";
import {Capacitor} from "@capacitor/core";

const SERVER_URL_KEY = 'mixserver_server_url';

export function getMixServerApiUrl(): string {
  if (Capacitor.isNativePlatform()) {
    return getStoredServerUrl();
  }

  const host = getMixServerApiHost();

  const scheme = environment.type === EnvironmentType.Production
    ? window.location.protocol
    : `${environment.apiProtocol}:`;

  return scheme + '//' + host;
}

export function getMixServerApiHost(): string {
  if (
    environment.type === EnvironmentType.Production &&
    window &&
    'location' in window &&
    'protocol' in window.location &&
    'host' in window.location
  ) {
    return window.location.host;
  }

  return environment.apiHost;
}

export function getStoredServerUrl(): string {
  return localStorage.getItem(SERVER_URL_KEY) ?? '';
}

export function setStoredServerUrl(url: string): void {
  localStorage.setItem(SERVER_URL_KEY, url);
}

export function hasStoredServerUrl(): boolean {
  const url = localStorage.getItem(SERVER_URL_KEY);
  return !!url && url.length > 0;
}
