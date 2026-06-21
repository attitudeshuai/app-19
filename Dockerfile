FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["LeftoverShare.sln", "./"]
COPY ["src/LeftoverShare.API/LeftoverShare.API.csproj", "src/LeftoverShare.API/"]
COPY ["tests/LeftoverShare.Tests/LeftoverShare.Tests.csproj", "tests/LeftoverShare.Tests/"]

RUN dotnet restore

COPY . .

RUN dotnet publish "src/LeftoverShare.API/LeftoverShare.API.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

EXPOSE 8089

ENV ASPNETCORE_URLS=http://+:8089
ENV ASPNETCORE_ENVIRONMENT=Production

HEALTHCHECK --interval=30s --timeout=5s --start-period=15s --retries=3 CMD curl -f http://localhost:8089/health || exit 1

ENTRYPOINT ["dotnet", "LeftoverShare.API.dll"]
