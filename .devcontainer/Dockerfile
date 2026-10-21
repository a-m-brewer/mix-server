# Development image mirrors the .NET and ffmpeg production toolchain.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS base

RUN apt-get update && \
    apt-get install -y --no-install-recommends \
        ffmpeg \
        git \
    && rm -rf /var/lib/apt/lists/*

FROM node:22-bookworm-slim AS node

FROM base
COPY --from=node /usr/local /usr/local

VOLUME /root/.nuget/packages
WORKDIR /workspace
