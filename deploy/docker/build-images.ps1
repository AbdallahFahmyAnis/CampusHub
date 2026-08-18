$ErrorActionPreference = "Stop"
$root = Resolve-Path "$PSScriptRoot/../.."
Set-Location $root

function Build-DotnetImage($name, $project) {
    docker build -f deploy/docker/dotnet.Dockerfile --build-arg PROJECT=$project -t campushub/${name}:local .
}

Build-DotnetImage identity src/services/identity/CampusHub.Identity.Api/CampusHub.Identity.Api.csproj
Build-DotnetImage catalog src/services/catalog/CampusHub.Catalog.Api/CampusHub.Catalog.Api.csproj
Build-DotnetImage enrollment src/services/enrollment/CampusHub.Enrollment.Api/CampusHub.Enrollment.Api.csproj
Build-DotnetImage payment src/services/payment/CampusHub.Payment.Api/CampusHub.Payment.Api.csproj
Build-DotnetImage notification src/services/notification/CampusHub.Notification.Api/CampusHub.Notification.Api.csproj
Build-DotnetImage access src/services/access/CampusHub.Access.Api/CampusHub.Access.Api.csproj
Build-DotnetImage gateway src/gateway/CampusHub.Gateway/CampusHub.Gateway.csproj

docker build -f deploy/docker/chat.Dockerfile -t campushub/chat:local .
docker build -f deploy/docker/web.Dockerfile -t campushub/web:local .

Write-Host "Images tagged campushub/*:local"
