ARG PROJECT_NAME=OpenFTTH.SpecificationImporter
ARG DOTNET_VERSION=8.0

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build-env

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
FROM mcr.microsoft.com/dotnet/runtime:${DOTNET_VERSION}-jammy-amd64

# Renew the ARG argument for it to be available in this build context.
ARG PROJECT_NAME

# Cannot reference ARG in CMD, so we set it in ENV instead.
ENV EXECUTEABLE=${PROJECT_NAME}.dll

WORKDIR /app

COPY --from=build-env /app/src/${PROJECT_NAME}/out .

# Cannot use PROJECT_NAME here in environment, have to sadly write out the whole name.
# There is a hack where you can execute this as an environment variable, but then the process won't have id 1
# and signal are no longer received.
ENTRYPOINT ["dotnet", "OpenFTTH.SpecificationImporter.dll"]