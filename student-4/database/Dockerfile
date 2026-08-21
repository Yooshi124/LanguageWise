# Database microservice for student 4 (Quests and Achievements).
#
# ASP.NET Core minimal API exposing CRUD over the SampleItems table.
# It is the only process that ever opens the SQLite file, which lives on a
# named Docker volume mounted at /data.
#
# NOTE: the build context is the repository root so that Directory.Build.props
#       and Directory.Packages.props can be restored.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG PROJECT=student-4/database/LanguageWise.Student4.Db
WORKDIR /src

# Restore first, using only the project files, so Docker can cache the layer.
COPY Directory.Build.props Directory.Packages.props ./
COPY ${PROJECT}/*.csproj ${PROJECT}/
RUN dotnet restore ${PROJECT}

COPY ${PROJECT}/ ${PROJECT}/
RUN dotnet publish ${PROJECT} -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# curl is used by the container health check below.
RUN apt-get update \
    && apt-get install --no-install-recommends -y curl \
    && rm -rf /var/lib/apt/lists/* \
    && mkdir -p /data \
    && chown app:app /data

COPY --from=build /app/publish ./

ENV ASPNETCORE_HTTP_PORTS=8080 \
    Database__Path=/data/student-4.db

USER app
EXPOSE 8080

HEALTHCHECK --interval=10s --timeout=3s --start-period=15s --retries=6 \
    CMD curl --fail --silent http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "LanguageWise.Student4.Db.dll"]
