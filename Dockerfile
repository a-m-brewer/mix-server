FROM mcr.microsoft.com/dotnet/aspnet:8.0-bookworm-slim AS base

RUN apt-get update && \
    apt-get install -y ffmpeg

WORKDIR /app
EXPOSE 5225

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG TARGETARCH
WORKDIR /src
COPY ["Directory.Build.props", "Directory.Build.props"]
COPY ["src/api/MixServer/MixServer.csproj", "src/api/MixServer/"]
COPY ["src/api/MixServer.Application/MixServer.Application.csproj", "src/api/MixServer.Application/"]
COPY ["src/api/MixServer.Domain/MixServer.Domain.csproj", "src/api/MixServer.Domain/"]
COPY ["src/api/MixServer.Infrastructure/MixServer.Infrastructure.csproj", "src/api/MixServer.Infrastructure/"]

RUN dotnet restore "src/api/MixServer/MixServer.csproj" -a "$TARGETARCH"

COPY ["src/api/MixServer/.", "src/api/MixServer/"]
COPY ["src/api/MixServer.Application/.", "src/api/MixServer.Application/"]
COPY ["src/api/MixServer.Domain/.", "src/api/MixServer.Domain/"]
COPY ["src/api/MixServer.Infrastructure/.", "src/api/MixServer.Infrastructure/"]

RUN dotnet build "src/api/MixServer/MixServer.csproj" -c Release -o /app/build -a "$TARGETARCH"

FROM build AS publish
RUN dotnet publish "src/api/MixServer/MixServer.csproj" -c Release -o /app/publish -a "$TARGETARCH"

# Use official node image as the base image
FROM node:20.18.3 AS build_client

# Set the working directory
WORKDIR /usr/local/app

# Add the source code to app
COPY src/clients/mix-server-client/ /usr/local/app/

# Install all the dependencies
RUN npm install -g @angular/cli
RUN npm ci

# Generate the build of the application
RUN npx ng build --configuration production

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=build_client /usr/local/app/dist/mix-server-client/browser ./wwwroot
ENTRYPOINT ["dotnet", "MixServer.dll"]
