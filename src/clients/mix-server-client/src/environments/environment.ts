import {IEnvironment} from "./environment.interface";
import {EnvironmentType} from "./environment-type.enum";

export const environment: IEnvironment = {
  type: EnvironmentType.Production,
  apiHost: '',
  apiProtocol: ''
};
