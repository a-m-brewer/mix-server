FROM mcr.microsoft.com/dotnet/aspnet:8.0-bookworm-slim AS base

WORKDIR /app
EXPOSE 5225

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Directory.Build.props", "Directory.Build.props"]
COPY ["src/api/MixServer/MixServer.csproj", "src/api/MixServer/"]
COPY ["src/api/MixServer.Application/MixServer.Application.csproj", "src/api/MixServer.Application/"]
COPY ["src/api/MixServer.Domain/MixServer.Domain.csproj", "src/api/MixServer.Domain/"]
COPY ["src/api/MixServer.Infrastructure/MixServer.Infrastructure.csproj", "src/api/MixServer.Infrastructure/"]

RUN dotnet restore "src/api/MixServer/MixServer.csproj"

COPY ["src/api/MixServer/.", "src/api/MixServer/"]
COPY ["src/api/MixServer.Application/.", "src/api/MixServer.Application/"]
COPY ["src/api/MixServer.Domain/.", "src/api/MixServer.Domain/"]
COPY ["src/api/MixServer.Infrastructure/.", "src/api/MixServer.Infrastructure/"]

RUN dotnet build "src/api/MixServer/MixServer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "src/api/MixServer/MixServer.csproj" -c Release -o /app/publish

# Use official node image as the base image
FROM node:20.10 as build_client

# Set the working directory
WORKDIR /usr/local/app

# Add the source code to app
COPY src/clients/mix-server-client/ /usr/local/app/

# Install all the dependencies
RUN npm install -g @angular/cli
RUN npm install

# Generate the build of the application
RUN npx ng build --configuration production

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=build_client /usr/local/app/dist/mix-server-client ./wwwroot
ENTRYPOINT ["dotnet", "MixServer.dll"]
