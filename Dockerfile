# No `# syntax=` directive on purpose: it makes BuildKit fetch its frontend from
# Docker Hub, which fails on a network that can only reach MCR. Nothing below needs
# a feature the built-in frontend lacks.

# ---------------------------------------------------------------- build ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

# .editorconfig is not optional here. Directory.Build.props sets
# TreatWarningsAsErrors alongside EnforceCodeStyleInBuild, and the .editorconfig is
# what turns rules like CA2007 and CA1848 down to silent. Without it in the build
# context the build fails on diagnostics the repository has deliberately relaxed.
COPY .editorconfig Directory.Build.props Directory.Packages.props ./

# Project files by themselves first, so a source-only change reuses the restore layer
COPY src/Atmos.Api/Atmos.Api.csproj                           src/Atmos.Api/
COPY src/Atmos.Common/Atmos.Common.csproj                     src/Atmos.Common/
COPY src/Atmos.Database/Atmos.Database.csproj                 src/Atmos.Database/
COPY src/Atmos.Domain/Atmos.Domain.csproj                     src/Atmos.Domain/
COPY src/Atmos.Services.Api/Atmos.Services.Api.csproj         src/Atmos.Services.Api/
COPY src/Atmos.Services.Default/Atmos.Services.Default.csproj src/Atmos.Services.Default/
COPY src/Atmos.Services.Media/Atmos.Services.Media.csproj     src/Atmos.Services.Media/
COPY src/Atmos.Templates/Atmos.Templates.csproj               src/Atmos.Templates/

RUN dotnet restore src/Atmos.Api/Atmos.Api.csproj

COPY src/ src/

RUN dotnet publish src/Atmos.Api/Atmos.Api.csproj \
        --configuration Release \
        --no-restore \
        --output /publish

# The runtime image has no shell, so a directory cannot be created and chowned
# there. Stage an empty one here and copy it across carrying its ownership.
RUN mkdir -p /media-root

# -------------------------------------------------------------- runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS runtime

# Inherited from the base image and deliberately left as-is:
#   USER 1654 (APP_UID)                        - never runs as root
#   ASPNETCORE_HTTP_PORTS=8080
#   DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true - chiseled carries no ICU
#
# Invariant globalization fits this code, which compares and cases with Ordinal or
# Invariant throughout and constructs no named cultures. AddLocalization() is
# registered in Atmos.Templates, but it has no resources and no caller. An email
# template that resolved a named culture would throw here; that needs the
# -noble-chiseled-extra tag, which ships ICU.

WORKDIR /app

COPY --from=build /publish ./

# Owned by the runtime user so MediaDirectoryInitializer can create the leaf
# directory beneath it at startup. Only the Local media provider touches this.
COPY --from=build --chown=$APP_UID:$APP_UID /media-root /var/lib/atmos/media

ENV ASPNETCORE_ENVIRONMENT=Production \
    Media__Local__RootPath=/var/lib/atmos/media

VOLUME ["/var/lib/atmos/media"]

EXPOSE 8080

# No HEALTHCHECK instruction: it needs a binary to run, and a chiseled image has
# neither a shell nor curl. One that could never succeed would be worse than none.
# Probe /alive for liveness and /health for readiness (it includes the database)
# from whatever runs the container.
ENTRYPOINT ["dotnet", "Atmos.Api.dll"]
