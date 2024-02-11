import {environment} from "../environments/environment";
import {EnvironmentType} from "../environments/environment-type.enum";

export function getMixServerApiUrl(): string {
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
