# HalalChain Marketplace MVC service.

# The .NET 10 SDK and ASP.NET runtime use *different* patch versions:
# the SDK 10.0.200 ships with the ASP.NET runtime 10.0.11. Pin both
# explicitly.
ARG DOTNET_SDK_VERSION=10.0.200
ARG DOTNET_RUNTIME_VERSION=10.0.11
FROM node:22-alpine AS styles
# wwwroot/scss/ is the source of truth; wwwroot/css/ is generated and is not
# tracked in git, so it has to be compiled here before publish.
WORKDIR /styles
COPY package.json package-lock.json ./
RUN npm install --no-audit --no-fund
COPY scripts/ ./scripts/
COPY HalalChain.Web/wwwroot/scss/ ./HalalChain.Web/wwwroot/scss/
COPY HalalChain.Marketplace/wwwroot/scss/ ./HalalChain.Marketplace/wwwroot/scss/
RUN node scripts/build-scss.mjs --silent
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_RUNTIME_VERSION} AS base
WORKDIR /app
EXPOSE 8080
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_SDK_VERSION} AS build
ARG PROJECT=HalalChain.Marketplace
ARG PROJECT_DLL=HalalChain.Marketplace
WORKDIR /src
COPY ["global.json", "Directory.Build.props", "Directory.Build.targets", "./"]
COPY ["HalalChain.Platform.Contracts/HalalChain.Platform.Contracts.csproj", "HalalChain.Platform.Contracts/"]
COPY ["HalalChain.Platform.Http/HalalChain.Platform.Http.csproj", "HalalChain.Platform.Http/"]
COPY ["${PROJECT}/${PROJECT}.csproj", "${PROJECT}/"]
RUN dotnet restore "HalalChain.Platform.Http/HalalChain.Platform.Http.csproj"
RUN dotnet restore "${PROJECT}/${PROJECT}.csproj"
COPY . .
RUN dotnet build "HalalChain.Platform.Http/HalalChain.Platform.Http.csproj" -c Release --no-restore
RUN dotnet build "${PROJECT}/${PROJECT}.csproj" -c Release --no-restore -o /app/build
FROM build AS publish
RUN dotnet publish "${PROJECT}/${PROJECT}.csproj" -c Release -o /app/publish /p:UseAppHost=false
# Overlay the stylesheets compiled in the styles stage. They are not part of
# the .NET publish output because the SDK only ships wwwroot content that was
# present at build time.
COPY --from=styles /styles/HalalChain.Marketplace/wwwroot/css/ /app/publish/wwwroot/css/
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "HalalChain.Marketplace.dll"]
