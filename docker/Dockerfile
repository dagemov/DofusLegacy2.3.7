# syntax=docker/dockerfile:1.7

FROM mcr.microsoft.com/dotnet/sdk:11.0-preview AS build
WORKDIR /src

COPY ["Sunshine net11.0/Sunshine net11.0/", "./"]

RUN dotnet restore "Sunshine.csproj"
RUN dotnet publish "Sunshine.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/runtime:11.0-preview AS final

RUN apt-get update \
    && apt-get install -y --no-install-recommends mariadb-client netcat-openbsd \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

COPY --from=build /app/publish/ /app/
COPY ["docker/entrypoint.sh", "/usr/local/bin/entrypoint.sh"]

RUN sed -i 's/\r$//' /usr/local/bin/entrypoint.sh \
    && chmod +x /usr/local/bin/entrypoint.sh \
    && mkdir -p /app/runtime /app/logs

ENTRYPOINT ["/usr/local/bin/entrypoint.sh"]
CMD ["dotnet", "Sunshine.dll", "--no-console-input"]
