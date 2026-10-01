ARG DOTNET_SDK_VERSION=10.0.200
ARG DOTNET_RUNTIME_VERSION=10.0.11
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_SDK_VERSION} AS build
WORKDIR /src
COPY . .
RUN dotnet restore HalalChain.Automation.csproj
RUN dotnet publish HalalChain.Automation.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_RUNTIME_VERSION} AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV DOTNET_ENVIRONMENT=Production
ENTRYPOINT ["dotnet", "HalalChain.Automation.dll"]
