#!/bin/sh

# Persist appsettings.json in the data volume so auto-generated keys
# (JWT secret, encryption key) survive container recreation.
if [ ! -f /app/data/appsettings.json ]; then
    cp /app/appsettings.example.json /app/data/appsettings.json
fi
ln -sf /app/data/appsettings.json /app/appsettings.json

exec dotnet EchoHub.Server.dll
