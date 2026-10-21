FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/LanguageWise.McpServer/*.csproj src/LanguageWise.McpServer/
RUN dotnet restore src/LanguageWise.McpServer
COPY src/LanguageWise.McpServer/ src/LanguageWise.McpServer/
RUN dotnet publish src/LanguageWise.McpServer -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
RUN apt-get update && apt-get install --no-install-recommends -y curl && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish ./
ENV Urls=http://0.0.0.0:8080
USER app
EXPOSE 8080
HEALTHCHECK --interval=10s --timeout=3s --start-period=15s --retries=6 CMD curl --fail --silent http://127.0.0.1:8080/health || exit 1
ENTRYPOINT ["dotnet", "LanguageWise.McpServer.dll"]
