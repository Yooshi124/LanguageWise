# Backend/API microservice for student 4 (Quests and Achievements).
#
# ASP.NET Core minimal API. It never touches SQLite directly — it calls the
# database microservice over HTTP on the internal Docker network.
#
# NOTE: the build context is the repository root so that Directory.Build.props
#       and Directory.Packages.props can be restored.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG PROJECT=student-4/backend/LanguageWise.Student4.Api
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props ./
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
    Services__Database=http://student-4-db:8080

USER app
EXPOSE 8080

HEALTHCHECK --interval=10s --timeout=3s --start-period=15s --retries=6 \
    CMD curl --fail --silent http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "LanguageWise.Student4.Api.dll"]
