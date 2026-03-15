#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR=${ROOT_DIR:-/opt/rollout}
ENV_FILE="$ROOT_DIR/.env.production"
BACKUP_DIR="$ROOT_DIR/backups"
TIMESTAMP=$(date +%F-%H%M%S)

set -a
source "$ENV_FILE"
set +a

mkdir -p "$BACKUP_DIR"

docker exec rollout-postgres pg_dump -U "$POSTGRES_USER" -d "$ROLLOUT_AUTH_DB" -Fc > "$BACKUP_DIR/auth-$TIMESTAMP.dump"
docker exec rollout-postgres pg_dump -U "$POSTGRES_USER" -d "$ROLLOUT_CORE_DB" -Fc > "$BACKUP_DIR/core-$TIMESTAMP.dump"

find "$BACKUP_DIR" -type f -name '*.dump' -mtime +7 -delete