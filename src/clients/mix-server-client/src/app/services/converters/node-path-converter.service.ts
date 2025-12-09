import { Injectable } from '@angular/core';
import {NodePathDto, NodePathHeaderDto, NodePathRequestDto} from "../../generated-clients/mix-server-clients";
import {NodePath, NodePathHeader} from "../../main-content/file-explorer/models/node-path";

@Injectable({
  providedIn: 'root'
})
export class NodePathConverterService {

  constructor() { }

  public fromDto(dto: NodePathDto): NodePath {
    return new NodePath(
      dto.rootPath!,
      dto.relativePath!,
      dto.fileName,
      dto.absolutePath,
      dto.extension,
      this.fromHeaderDto(dto.parent),
      dto.isRoot,
      dto.isRootChild
    );
  }

  public fromHeaderDto(dto: NodePathHeaderDto): NodePathHeader {
    return new NodePathHeader(
      dto.rootPath!,
      dto.relativePath!
    );
  }

  public toRequestDto(nodePathHeader: NodePathHeader): NodePathRequestDto {
    return new NodePathRequestDto({
      rootPath: nodePathHeader.rootPath,
      relativePath: nodePathHeader.relativePath
    });
  }
}
