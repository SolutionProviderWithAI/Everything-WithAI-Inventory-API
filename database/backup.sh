#!/usr/bin/env bash
# ==============================================================================
# Database Backup Script for PostgreSQL (Linux / Docker)
# ==============================================================================
set -e

HOST="${POSTGRES_HOST:-localhost}"
PORT="${POSTGRES_PORT:-5432}"
DB="${POSTGRES_DB:-inventory_db}"
USER="${POSTGRES_USER:-postgres}"
BACKUP_DIR="./backups"
TIMESTAMP=$(date +"%Y%m%d_%H%M%S")

mkdir -p "$BACKUP_DIR"
BACKUP_FILE="$BACKUP_DIR/${DB}_backup_${TIMESTAMP}.sql"

echo "Backing up database $DB from $HOST:$PORT..."
export PGPASSWORD="${POSTGRES_PASSWORD:-postgres}"
pg_dump -h "$HOST" -p "$PORT" -U "$USER" -F p -b -v -f "$BACKUP_FILE" "$DB"

echo "Backup created successfully: $BACKUP_FILE"
