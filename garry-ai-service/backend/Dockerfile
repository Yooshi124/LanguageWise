FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY LanguageWise.GarryAiService.Api/*.csproj LanguageWise.GarryAiService.Api/
RUN dotnet restore LanguageWise.GarryAiService.Api
COPY LanguageWise.GarryAiService.Api/ LanguageWise.GarryAiService.Api/
RUN dotnet publish LanguageWise.GarryAiService.Api -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
RUN apt-get update && apt-get install --no-install-recommends -y curl && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish ./
ENV ASPNETCORE_HTTP_PORTS=8080
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "LanguageWise.GarryAiService.Api.dll"]