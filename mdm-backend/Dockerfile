FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS build
WORKDIR /src
COPY src/Hestia.LocationsMDM.WebApi .

WORKDIR /src
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/core/aspnet:latest AS final
WORKDIR /app

# install System.Drawing native dependencies
RUN apt-get update \
    && apt-get install -y --allow-unauthenticated \
        libc6-dev \
        libgdiplus \
        libx11-dev \
     && rm -rf /var/lib/apt/lists/*

# install datadog agent
RUN apt-get update -y
RUN apt-get install wget -y
RUN wget https://github.com/DataDog/dd-trace-dotnet/releases/download/v2.20.0/datadog-dotnet-apm_2.20.0_amd64.deb -O /opt/datadog-dotnet-apm_2.20.0_amd64.deb
RUN apt-get install -f /opt/datadog-dotnet-apm_2.20.0_amd64.deb -y

ENV ASPNETCORE_URLS=http://localhost:8080

COPY --from=build /app .
ENTRYPOINT ["dotnet", "Hestia.LocationsMDM.WebApi.dll"]
