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
# build-scss.mjs locates the repo root by walking up to this marker file, so it
# has to exist in the (deliberately minimal) styles context.
COPY HalalChain.Platform.sln ./
COPY infrastructure/scripts/ ./infrastructure/scripts/
COPY HalalChain.Web/wwwroot/scss/ ./HalalChain.Web/wwwroot/scss/
COPY HalalChain.Marketplace/wwwroot/scss/ ./HalalChain.Marketplace/wwwroot/scss/
RUN node infrastructure/scripts/build-scss.mjs --silent
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_RUNTIME_VERSION} AS base
WORKDIR /app
EXPOSE 8080
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_SDK_VERSION} AS build
ARG PROJECT=HalalChain.Marketplace
ARG PROJECT_DLL=HalalChain.Marketplace
WORKDIR /src
COPY ["global.json", "Directory.Build.props", "Directory.Build.targets", "./"]
COPY ["HalalChain.Domain/HalalChain.Domain.csproj", "HalalChain.Domain/"]
COPY ["HalalChain.Application/HalalChain.Application.csproj", "HalalChain.Application/"]
COPY ["HalalChain.Platform.Contracts/HalalChain.Platform.Contracts.csproj", "HalalChain.Platform.Contracts/"]
COPY ["HalalChain.Platform.Http/HalalChain.Platform.Http.csproj", "HalalChain.Platform.Http/"]
COPY ["${PROJECT}/${PROJECT}.csproj", "${PROJECT}/"]
# Restore from the leaves inward. ${PROJECT} references Domain, Application,
# Contracts and Http; Application in turn references Domain and Contracts. A
# ProjectReference whose .csproj was not copied above is silently skipped by
# restore and then fails the build with NETSDK1004, so every project in that
# closure needs a COPY and a restore line here.
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
# Overlay the stylesheets compiled in the styles stage. They are not part of
# the .NET publish output because the SDK only ships wwwroot content that was
# present at build time.
COPY --from=styles /styles/HalalChain.Marketplace/wwwroot/css/ /app/publish/wwwroot/css/
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "HalalChain.Marketplace.dll"]
