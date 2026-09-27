# HalalChain Platform API image.

# The .NET 10 SDK and ASP.NET runtime use *different* patch versions:
# the SDK 10.0.200 ships with the ASP.NET runtime 10.0.11. Pin both
# explicitly so a future SDK release cannot silently change the runtime
# version.
ARG DOTNET_SDK_VERSION=10.0.200
ARG DOTNET_RUNTIME_VERSION=10.0.11
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_RUNTIME_VERSION} AS base
WORKDIR /app
EXPOSE 8080
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_SDK_VERSION} AS build
ARG PROJECT=HalalChain.Platform.Api
ARG PROJECT_DLL=HalalChain.Platform.Api
WORKDIR /src
COPY ["global.json", "Directory.Build.props", "Directory.Build.targets", "./"]
COPY ["HalalChain.Domain/HalalChain.Domain.csproj", "HalalChain.Domain/"]
COPY ["HalalChain.Application/HalalChain.Application.csproj", "HalalChain.Application/"]
COPY ["HalalChain.Platform.Contracts/HalalChain.Platform.Contracts.csproj", "HalalChain.Platform.Contracts/"]
COPY ["HalalChain.Platform.Http/HalalChain.Platform.Http.csproj", "HalalChain.Platform.Http/"]
COPY ["${PROJECT}/${PROJECT}.csproj", "${PROJECT}/"]
# Restore the project reference graph from the leaves inward (Http depends on
# Contracts; Application depends on Domain; this project depends on all of
# them). Every ProjectReference of ${PROJECT} must have its .csproj copied
# above or restore silently skips it with a "Skipping project" warning and
# the build fails later with NETSDK1004.
RUN dotnet restore "HalalChain.Domain/HalalChain.Domain.csproj"
RUN dotnet restore "HalalChain.Application/HalalChain.Application.csproj"
RUN dotnet restore "HalalChain.Platform.Http/HalalChain.Platform.Http.csproj"
RUN dotnet restore "${PROJECT}/${PROJECT}.csproj"
COPY . .
RUN dotnet build "HalalChain.Domain/HalalChain.Domain.csproj" -c Release --no-restore
RUN dotnet build "HalalChain.Application/HalalChain.Application.csproj" -c Release --no-restore
RUN dotnet build "HalalChain.Platform.Http/HalalChain.Platform.Http.csproj" -c Release --no-restore
RUN dotnet build "${PROJECT}/${PROJECT}.csproj" -c Release --no-restore -o /app/build
FROM build AS publish
RUN dotnet publish "${PROJECT}/${PROJECT}.csproj" -c Release -o /app/publish /p:UseAppHost=false
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "HalalChain.Platform.Api.dll"]
