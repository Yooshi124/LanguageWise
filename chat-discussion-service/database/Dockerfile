# Database service container for chat-discussion-service.
# Build context is this folder only.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG PROJECT=LanguageWise.ChatDiscussionService.Db
WORKDIR /src

COPY ${PROJECT}/*.csproj ${PROJECT}/
RUN dotnet restore ${PROJECT}

COPY ${PROJECT}/ ${PROJECT}/
RUN dotnet publish ${PROJECT} -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN apt-get update \
    && apt-get install --no-install-recommends -y curl \
    && rm -rf /var/lib/apt/lists/* \
    && mkdir -p /data \
    && chown -R app:app /data

COPY --from=build /app/publish ./

ENV ASPNETCORE_HTTP_PORTS=8080 \
    Database__Path=/data/chat-discussion-service.db

VOLUME ["/data"]

USER app
EXPOSE 8080

HEALTHCHECK --interval=10s --timeout=3s --start-period=15s --retries=6 \
    CMD curl --fail --silent http://127.0.0.1:8080/health || exit 1

ENTRYPOINT ["dotnet", "LanguageWise.ChatDiscussionService.Db.dll"]
