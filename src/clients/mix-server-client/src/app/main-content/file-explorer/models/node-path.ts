export class NodePathHeader {
  constructor(public rootPath: string,
              public relativePath: string)
  {
  }

  // this is not the absolute path, but the key to identify the node
  // As the client should not need the logic to know different platforms path separators
  public get key(): string {
    return this.rootPath + ";" + this.relativePath;
  }

  public get empty(): boolean {
    return this.rootPath === '' && this.relativePath === '';
  }

  public static get Default(): NodePathHeader {
    return new NodePathHeader('', '');
  }

  public copy(): NodePathHeader {
    return new NodePathHeader(this.rootPath, this.relativePath);
  }

  public isEqual(nodePathParent: NodePathHeader | null | undefined): boolean {
    if (!nodePathParent) {
      return false;
    }

    return this.rootPath === nodePathParent.rootPath && this.relativePath === nodePathParent.relativePath;
  }
}

export class NodePath extends NodePathHeader {
  constructor(rootPath: string,
              relativePath: string,
              public fileName: string,
              public absolutePath: string,
              public extension: string,
              public parent: NodePathHeader,
              public isRoot: boolean,
              public isRootChild: boolean) {
    super(rootPath, relativePath);
  }

  public static override get Default(): NodePath {
    return new NodePath('', '', '', '', '', NodePathHeader.Default, false, false);
  }

  public override copy(): NodePath {
    return new NodePath(this.rootPath, this.relativePath, this.fileName, this.absolutePath, this.extension, this.parent.copy(), this.isRoot, this.isRootChild);
  }
}
