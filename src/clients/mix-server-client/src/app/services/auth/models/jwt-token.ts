export interface IJwtToken {
  get value(): string,
  get expires(): Date;
  get expired(): boolean;
}

export class JwtToken implements IJwtToken {
  private _token: any;

  private readonly _userId: string;
  private readonly _username: string;
  private readonly _expires: Date;

  constructor(private _value: string) {
    const jwtBase64 = this._value.split('.')[1];
    this._token = JSON.parse(atob(jwtBase64));
    this._expires = new Date(this._token.exp * 1000);
    this._userId = this._token['UserId'];
    this._username = this._token['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'];
  }

  public get value(): string {
    return this._value;
  }

  public get userId(): string {
    return this._userId;
  }

  public get username(): string {
    return this._username;
  }

  public get expires(): Date {
    return this._expires;
  }

  public get expired(): boolean {
    const expiresMilliseconds = this._expires.getTime();
    const nowMilliseconds = Date.now();

    const expired = expiresMilliseconds <= nowMilliseconds;
    return expired;
  }
}
