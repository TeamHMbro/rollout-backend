#!/usr/bin/env bash
set -euo pipefail

TAG=${1:?image tag is required}
ROOT_DIR=${ROOT_DIR:-/opt/rollout}
ENV_FILE="$ROOT_DIR/.env.production"
COMPOSE_FILE="$ROOT_DIR/docker-compose.yml"

mkdir -p "$ROOT_DIR/deploy/postgres/init"

if grep -q '^ROLLOUT_IMAGE_TAG=' "$ENV_FILE"; then
  sed -i "s/^ROLLOUT_IMAGE_TAG=.*/ROLLOUT_IMAGE_TAG=$TAG/" "$ENV_FILE"
else
  printf '\nROLLOUT_IMAGE_TAG=%s\n' "$TAG" >> "$ENV_FILE"
fi

set -a
source "$ENV_FILE"
set +a

echo "$GHCR_TOKEN" | docker login ghcr.io -u "$GHCR_USERNAME" --password-stdin

docker compose --env-file "$ENV_FILE" -f "$COMPOSE_FILE" pull
docker compose --env-file "$ENV_FILE" -f "$COMPOSE_FILE" up -d --remove-orphans
docker image prune -f