# Backend/API container for quests-achievements-notifications-service.
#
# ASP.NET Core minimal API. It never opens SQLite directly; it calls this
# service's database container over HTTP.
#
# Build context is this folder, so the image cannot reach other microservices.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG PROJECT=LanguageWise.QuestsAchievementsNotificationsService.Api
WORKDIR /src

COPY ${PROJECT}/*.csproj ${PROJECT}/
RUN dotnet restore ${PROJECT}

COPY ${PROJECT}/ ${PROJECT}/
RUN dotnet publish ${PROJECT} -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN apt-get update \
    && apt-get install --no-install-recommends -y curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish ./

ENV ASPNETCORE_HTTP_PORTS=8080 \
    Services__Database=http://quests-achievements-notifications-service-db:8080

USER app
EXPOSE 8080

HEALTHCHECK --interval=10s --timeout=3s --start-period=15s --retries=6 \
    CMD curl --fail --silent http://127.0.0.1:8080/health || exit 1

ENTRYPOINT ["dotnet", "LanguageWise.QuestsAchievementsNotificationsService.Api.dll"]