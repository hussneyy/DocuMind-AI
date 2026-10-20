# Build context is the repository root (see docker-compose.yml) because the Api project
# references Domain/Application/Infrastructure via ProjectReference.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first, with only the project files, so this layer is cached across code-only changes.
COPY DocuMind.sln ./
COPY src/DocuMind.Domain/DocuMind.Domain.csproj src/DocuMind.Domain/
COPY src/DocuMind.Application/DocuMind.Application.csproj src/DocuMind.Application/
COPY src/DocuMind.Infrastructure/DocuMind.Infrastructure.csproj src/DocuMind.Infrastructure/
COPY src/DocuMind.Api/DocuMind.Api.csproj src/DocuMind.Api/
RUN dotnet restore src/DocuMind.Api/DocuMind.Api.csproj

COPY src/DocuMind.Domain/ src/DocuMind.Domain/
COPY src/DocuMind.Application/ src/DocuMind.Application/
COPY src/DocuMind.Infrastructure/ src/DocuMind.Infrastructure/
COPY src/DocuMind.Api/ src/DocuMind.Api/
RUN dotnet publish src/DocuMind.Api/DocuMind.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Private, non-web-accessible upload storage — same role as ./storage in local dev, just inside
# the container's filesystem; docker-compose.yml mounts a named volume here for persistence.
RUN mkdir -p /app/storage

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "DocuMind.Api.dll"]
