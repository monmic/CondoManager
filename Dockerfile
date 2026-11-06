FROM node:24-bookworm-slim AS web
WORKDIR /web
COPY src/frontend/web/package*.json ./
RUN npm ci
COPY src/frontend/web/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS backend
WORKDIR /source
COPY src/backend/ ./
RUN dotnet publish CondoManager.Api/CondoManager.Api.csproj -c Release -o /publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0-bookworm-slim
WORKDIR /app
COPY --from=backend /publish/ ./
COPY --from=web /web/dist/ ./frontend/
RUN mkdir -p /var/condomanager/documents /var/condomanager/keys && chown -R app:app /var/condomanager
ENV Frontend__RootPath=/app/frontend Documents__RootPath=/var/condomanager/documents DataProtection__KeyPath=/var/condomanager/keys ASPNETCORE_URLS=https://+:8443
USER app
EXPOSE 8443
ENTRYPOINT ["dotnet", "CondoManager.Api.dll"]
