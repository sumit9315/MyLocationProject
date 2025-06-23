#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Install Datadog agent
RUN apt-get update -y
RUN apt-get install wget -y
RUN wget https://github.com/DataDog/dd-trace-dotnet/releases/download/v2.20.0/datadog-dotnet-apm_2.20.0_amd64.deb -O /opt/datadog-dotnet-apm_2.20.0_amd64.deb
RUN apt-get install -f /opt/datadog-dotnet-apm_2.20.0_amd64.deb -y

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Hestia.LocationsMDM.WebApi/Hestia.LocationsMDM.WebApi.csproj", "src/Hestia.LocationsMDM.WebApi/"]
RUN dotnet restore "src/Hestia.LocationsMDM.WebApi/Hestia.LocationsMDM.WebApi.csproj"
COPY . .
WORKDIR "/src/src/Hestia.LocationsMDM.WebApi"
RUN dotnet build "Hestia.LocationsMDM.WebApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Hestia.LocationsMDM.WebApi.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Hestia.LocationsMDM.WebApi.dll"]