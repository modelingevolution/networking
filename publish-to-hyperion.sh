#!/bin/bash
set -e

SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
cd "$SCRIPT_DIR"

HOST="hyperion"
REMOTE_DIR="~/networkmanager"
APP_NAME="networkmanager-app"
PORT=5176

echo "Building and publishing for linux-x64 (Debug)..."
rm -rf ./publish
dotnet publish src/ModelingEvolution.NetworkManager.App/ModelingEvolution.NetworkManager.App.csproj \
    -c Debug \
    -r linux-x64 \
    --self-contained \
    -o ./publish

echo "Deploying to $HOST..."
rsync -avz --delete ./publish/ "$HOST:$REMOTE_DIR/"

echo "Stopping existing app..."
# Use pgrep to find PIDs first, then kill them (avoids killing own bash process)
ssh "$HOST" "sudo pkill -f '[n]etworkmanager-app' || true"
sleep 1

echo "Starting app on port $PORT..."
ssh -f "$HOST" "cd $REMOTE_DIR && sudo ASPNETCORE_URLS='http://0.0.0.0:$PORT' nohup ./$APP_NAME > /tmp/networkmanager.log 2>&1 &"
sleep 3

echo "Checking if app is running..."
set +e
ssh "$HOST" "pgrep -f $APP_NAME > /dev/null"
RUNNING=$?
set -e

if [ $RUNNING -eq 0 ]; then
    echo "App is running!"
    ssh "$HOST" "cat /tmp/networkmanager.log | grep -i 'listening'; exit 0"
    echo ""
    echo "Access at: http://192.168.30.32:$PORT"
else
    echo "ERROR: App failed to start!"
    ssh "$HOST" "cat /tmp/networkmanager.log | tail -20"
    exit 1
fi
