ARG PROJECT_NAME=OpenFTTH.SpecificationImporter
ARG DOTNET_VERSION=10.0

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION}-alpine AS build-env

# Renew the ARG argument for it to be available in this build context.
ARG PROJECT_NAME

WORKDIR /app

COPY ./*sln ./

COPY ./src/**/*.csproj ./src/${PROJECT_NAME}/

RUN dotnet restore --packages ./packages

COPY . ./
WORKDIR /app/src/${PROJECT_NAME}
RUN dotnet publish -c Release -o out --packages ./packages

# Build runtime image
FROM mcr.microsoft.com/dotnet/runtime:${DOTNET_VERSION}-alpine

# Renew the ARG argument for it to be available in this build context.
ARG PROJECT_NAME

RUN apk add --no-cache bash curl pipx

ENV PIPX_HOME=/opt/pipx \
    PIPX_BIN_DIR=/usr/local/bin
ENV PATH="$PIPX_BIN_DIR:$PATH"

RUN pipx install check-jsonschema

WORKDIR /app

COPY --from=build-env --chown=app:app /app/src/${PROJECT_NAME}/out .
COPY --chown=app:app specifications-schema.json .

USER app

# Cannot use PROJECT_NAME here in environment, have to sadly write out the whole name.
# There is a hack where you can execute this as an environment variable, but then the process won't have id 1
# and signal are no longer received.
ENTRYPOINT ["dotnet", "OpenFTTH.SpecificationImporter.dll"]
