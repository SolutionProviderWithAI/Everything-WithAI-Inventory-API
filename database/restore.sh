#!/usr/bin/env bash
# ==============================================================================
# Database Restore Script for PostgreSQL (Linux / Docker)
# ==============================================================================
set -e

BACKUP_FILE="$1"
if [ -z "$BACKUP_FILE" ] || [ ! -f "$BACKUP_FILE" ]; then
    echo "Usage: ./restore.sh <path-to-sql-backup>"
    exit 1
fi

HOST="${POSTGRES_HOST:-localhost}"
PORT="${POSTGRES_PORT:-5432}"
DB="${POSTGRES_DB:-inventory_db}"
USER="${POSTGRES_USER:-postgres}"

echo "Restoring database $DB from $BACKUP_FILE..."
export PGPASSWORD="${POSTGRES_PASSWORD:-postgres}"
psql -h "$HOST" -p "$PORT" -U "$USER" -d "$DB" -f "$BACKUP_FILE"

echo "Database restored successfully."
