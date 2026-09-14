FROM node:22-alpine AS frontend
WORKDIR /web
COPY src/frontend/office-system-web/package.json src/frontend/office-system-web/package-lock.json ./
RUN npm ci
COPY src/frontend/office-system-web/ ./
RUN npm run build -- --configuration production

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS backend
WORKDIR /src
COPY Directory.Build.props ./
COPY src/backend/ src/backend/
RUN dotnet publish src/backend/OfficeSystem.Api/OfficeSystem.Api.csproj \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=backend /app/publish ./
COPY --from=frontend /web/dist/office-system-web/browser ./wwwroot

ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080
USER $APP_UID

ENTRYPOINT ["dotnet", "OfficeSystem.Api.dll"]
