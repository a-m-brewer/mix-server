import {IEnvironment} from "./environment.interface";
import {EnvironmentType} from "./environment-type.enum";

export const environment: IEnvironment = {
  type: EnvironmentType.Development,
  apiHost: '192.168.1.100:5225',
  apiProtocol: 'http'
};
