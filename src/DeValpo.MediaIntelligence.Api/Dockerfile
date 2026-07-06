FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY DeValpo.MediaIntelligence.sln ./
COPY src/DeValpo.MediaIntelligence.Api/*.csproj src/DeValpo.MediaIntelligence.Api/
COPY src/DeValpo.MediaIntelligence.Application/*.csproj src/DeValpo.MediaIntelligence.Application/
COPY src/DeValpo.MediaIntelligence.Contracts/*.csproj src/DeValpo.MediaIntelligence.Contracts/
COPY src/DeValpo.MediaIntelligence.Domain/*.csproj src/DeValpo.MediaIntelligence.Domain/
COPY src/DeValpo.MediaIntelligence.Infrastructure/*.csproj src/DeValpo.MediaIntelligence.Infrastructure/
COPY tests/DeValpo.MediaIntelligence.Tests/*.csproj tests/DeValpo.MediaIntelligence.Tests/

RUN dotnet restore DeValpo.MediaIntelligence.sln

COPY . .
RUN dotnet publish src/DeValpo.MediaIntelligence.Api/DeValpo.MediaIntelligence.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "DeValpo.MediaIntelligence.Api.dll"]
