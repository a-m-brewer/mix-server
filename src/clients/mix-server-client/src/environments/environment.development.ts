import {IEnvironment} from "./environment.interface";
import {EnvironmentType} from "./environment-type.enum";

export const environment: IEnvironment = {
  type: EnvironmentType.Development,
  apiHost: 'localhost:5225',
  apiProtocol: 'http'
};
