import {EnvironmentType} from "./environment-type.enum";

export interface IEnvironment {
  type: EnvironmentType,
  apiHost: string,
  apiProtocol: string
}
