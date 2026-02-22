FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first for layer caching
COPY EchoHub.Core/EchoHub.Core.csproj EchoHub.Core/
COPY EchoHub.Server.Irc/EchoHub.Server.Irc.csproj EchoHub.Server.Irc/
COPY EchoHub.Server/EchoHub.Server.csproj EchoHub.Server/
COPY Directory.Build.props .
RUN dotnet restore EchoHub.Server/EchoHub.Server.csproj

# Copy everything and publish
COPY EchoHub.Core/ EchoHub.Core/
COPY EchoHub.Server.Irc/ EchoHub.Server.Irc/
COPY EchoHub.Server/ EchoHub.Server/
RUN dotnet publish EchoHub.Server/EchoHub.Server.csproj -c Release -o /app/publish --no-restore

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN groupadd -r echohub && useradd -r -g echohub -d /app echohub \
    && mkdir -p /app/data \
    && chown -R echohub:echohub /app

COPY --from=build --chown=echohub:echohub /app/publish .

USER echohub

ENV ASPNETCORE_ENVIRONMENT=Production \
    Urls=http://0.0.0.0:5000 \
    ConnectionStrings__DefaultConnection="Data Source=/app/data/echohub.db" \
    Storage__Path=/app/data/uploads \
    Serilog__WriteTo__1__Args__path=/app/data/logs/echohub-server-.log

EXPOSE 5000 6667 6697

ENTRYPOINT ["dotnet", "EchoHub.Server.dll"]
