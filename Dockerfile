# Multi-arch: docker buildx build --platform linux/amd64,linux/arm64 .
# BUILDPLATFORM keeps the compiler native; TARGETARCH decides what the image runs on.
# No `# syntax=` directive: it fetches a frontend from Docker Hub before FROM is read.
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build

ARG TARGETARCH

WORKDIR /src

# .editorconfig is required: with TreatWarningsAsErrors and EnforceCodeStyleInBuild,
# the severities it relaxes are load-bearing.
COPY .editorconfig Directory.Build.props Directory.Packages.props ./

# Project files first, so a source-only change reuses the restore layer
COPY src/Atmos.Api/Atmos.Api.csproj                           src/Atmos.Api/
COPY src/Atmos.Common/Atmos.Common.csproj                     src/Atmos.Common/
COPY src/Atmos.Database/Atmos.Database.csproj                 src/Atmos.Database/
COPY src/Atmos.Domain/Atmos.Domain.csproj                     src/Atmos.Domain/
COPY src/Atmos.Services.Api/Atmos.Services.Api.csproj         src/Atmos.Services.Api/
COPY src/Atmos.Services.Default/Atmos.Services.Default.csproj src/Atmos.Services.Default/
COPY src/Atmos.Services.Media/Atmos.Services.Media.csproj     src/Atmos.Services.Media/
COPY src/Atmos.Templates/Atmos.Templates.csproj               src/Atmos.Templates/

# RID-specific, or the --no-restore publish below cannot find the runtime pack
RUN RID="linux-$(case "$TARGETARCH" in \
            amd64) echo x64 ;; \
            arm64) echo arm64 ;; \
            *) echo "unsupported TARGETARCH '$TARGETARCH'" >&2; exit 1 ;; \
        esac)" \
    && echo "$RID" > /tmp/rid \
    && dotnet restore src/Atmos.Api/Atmos.Api.csproj --runtime "$RID"

COPY src/ src/

# Not trimmed: EF Core and the OpenAPI generator reflect past what a trimmer can see
RUN dotnet publish src/Atmos.Api/Atmos.Api.csproj \
        --configuration Release \
        --runtime "$(cat /tmp/rid)" \
        --self-contained true \
        --no-restore \
        --output /publish

FROM debian:trixie-slim AS runtime

# debian-slim ships none of these; the names are trixie's
RUN apt-get update \
    && apt-get install --no-install-recommends --yes \
        ca-certificates \
        libicu76 \
        libssl3t64 \
    && rm -rf /var/lib/apt/lists/*

# Nothing sets these here. Without ASPNETCORE_HTTP_PORTS, Kestrel listens on 5000.
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    Media__Local__RootPath=/var/lib/atmos/media

RUN groupadd --gid 1000 app \
    && useradd --uid 1000 --gid 1000 --no-create-home --shell /usr/sbin/nologin app \
    && mkdir -p /app /var/lib/atmos/media \
    && chown -R 1000:1000 /app /var/lib/atmos/media

WORKDIR /app

COPY --from=build --chown=1000:1000 /publish ./

USER 1000

VOLUME ["/var/lib/atmos/media"]

EXPOSE 8080

# No HEALTHCHECK: no HTTP client in the image. Probe /alive and /health externally.
ENTRYPOINT ["./Atmos.Api"]
