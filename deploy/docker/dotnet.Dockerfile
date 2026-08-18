# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY Directory.Build.props Directory.Packages.props CampusHub.sln ./
COPY contracts ./contracts
COPY src ./src
RUN dotnet restore CampusHub.sln
ARG PROJECT
RUN dotnet publish ${PROJECT} -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
RUN mkdir -p /data
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_EnableDiagnostics=0
EXPOSE 8080
# Override CMD in Kubernetes with the service DLL, e.g. CampusHub.Identity.Api.dll
ENTRYPOINT ["dotnet"]
