FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copiar arquivos de projeto
COPY ["src/AgroSolutions.Monitoracao.Api/AgroSolutions.Monitoracao.Api.csproj", "src/AgroSolutions.Monitoracao.Api/"]
COPY ["src/AgroSolutions.Monitoracao.Aplicacao/AgroSolutions.Monitoracao.Aplicacao.csproj", "src/AgroSolutions.Monitoracao.Aplicacao/"]
COPY ["src/AgroSolutions.Monitoracao.Dominio/AgroSolutions.Monitoracao.Dominio.csproj", "src/AgroSolutions.Monitoracao.Dominio/"]
COPY ["src/AgroSolutions.Monitoracao.Infra/AgroSolutions.Monitoracao.Infra.csproj", "src/AgroSolutions.Monitoracao.Infra/"]

# Restaurar dependências
RUN dotnet restore "src/AgroSolutions.Monitoracao.Api/AgroSolutions.Monitoracao.Api.csproj"

# Copiar código-fonte
COPY src/ src/

# Build
RUN dotnet publish -c Release -o /app/publish "src/AgroSolutions.Monitoracao.Api/AgroSolutions.Monitoracao.Api.csproj"

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "AgroSolutions.Monitoracao.Api.dll"]
