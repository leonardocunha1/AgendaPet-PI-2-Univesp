# ---------- 1. Build do front-end (React) ----------
FROM node:20-alpine AS web
WORKDIR /web
COPY web/package*.json ./
RUN npm ci || npm install
COPY web/ ./
# o vite gera em ../src/AgendaPet.Api/wwwroot
RUN mkdir -p /src/AgendaPet.Api && npm run build

# ---------- 2. Build e testes da API (.NET) ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app
COPY AgendaPet.sln ./
COPY src/AgendaPet.Api/AgendaPet.Api.csproj src/AgendaPet.Api/
COPY tests/AgendaPet.Tests/AgendaPet.Tests.csproj tests/AgendaPet.Tests/
RUN dotnet restore AgendaPet.sln
COPY src/ src/
COPY tests/ tests/
COPY --from=web /src/AgendaPet.Api/wwwroot/ src/AgendaPet.Api/wwwroot/
RUN dotnet test tests/AgendaPet.Tests -c Release --no-restore
RUN dotnet publish src/AgendaPet.Api -c Release -o /out --no-restore

# ---------- 3. Imagem final (só o runtime) ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /out ./
ENV ASPNETCORE_URLS=http://+:5000 \
    ConnectionStrings__Padrao="Data Source=/data/agendapet.db"
RUN mkdir -p /data
VOLUME /data
EXPOSE 5000
ENTRYPOINT ["dotnet", "AgendaPet.Api.dll"]
