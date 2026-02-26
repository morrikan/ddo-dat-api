#!/bin/bash

# Parse arguments
USE_DOCKER=false
for arg in "$@"; do
  case "$arg" in
    --docker) USE_DOCKER=true ;;
  esac
done

REG_PATH="HKLM\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\bc8a6440-918f-11dd-ad8b-0800200c9a66_is1"
VALUE_KEY="InstallLocation"
INSTALL_PATH=$(reg query "$REG_PATH" -v "$VALUE_KEY" 2>/dev/null | sed -n 's/.*REG_SZ\s*//p' | tr -d '\r')

if [ "$USE_DOCKER" = false ]; then
  if [ -z "$INSTALL_PATH" ]; then
    # the application uses the same registry check as here. if it fails here, it will fail in the app.
    echo "Could not find DDO installation folder. You need to open `DatSource.cs` and set it manually."
    exit 1;
  fi

  echo "Building / Running the API project in a new window..."
  start "DDO Dat API" bash -c "dotnet run --project ./src/DdoDatApi/DdoDatApi.csproj -c DEBUG 2>&1 | tee ddodatapi.log.txt"
  echo "dotnet running in separate window (log: ddodatapi.log.txt)"
else
  echo "Building / Running the project in docker..."

  # INSTALL_PATH="/c/your/path/here"
  if [ -z "$INSTALL_PATH" ]; then
    echo "Could not find DDO installation folder. Open `run.sh` and set it manually."
    exit 1;
  fi

  echo "DDO installation found at: $INSTALL_PATH"
  echo "DDO_INSTALL_PATH=$INSTALL_PATH" > .env

  docker rm -f dat-api-server 2>/dev/null
  docker build -t ddodatapi src/DdoDatApi/
  docker run --name dat-api-server --env-file .env -v "$INSTALL_PATH:/data" -d -p 5138:8080 ddodatapi
fi

TIMEOUT=30
ELAPSED=0
until curl -s -o /dev/null -w '%{http_code}' http://localhost:5138/swagger/v1/swagger.json | grep -q 200; do
  if [ "$ELAPSED" -ge "$TIMEOUT" ]; then
    echo "API failed to start within ${TIMEOUT}s. Check ddodatapi.log.txt or docker logs."
    exit 1
  fi
  echo "Waiting for API to start... (${ELAPSED}s/${TIMEOUT}s)"
  sleep 1
  ELAPSED=$((ELAPSED + 1))
done

curl -s http://localhost:5138/swagger/v1/swagger.json -o openapi3.json
echo "Saved OpenAPI schema to openapi3.json"

start http://localhost:5138/swagger

claude mcp remove ddo-dat-api
claude mcp add ddo-dat-api -- cmd //c npx -y mcp-openapi-schema openapi3.json --api-base-url http://localhost:5138/

claude

if [ "$USE_DOCKER" = true ]; then
  echo "Stopping docker container..."
  docker stop dat-api-server 2>/dev/null
fi
echo "API window can be closed manually, or will stop when its window is closed."