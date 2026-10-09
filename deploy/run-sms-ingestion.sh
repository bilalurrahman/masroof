#!/usr/bin/env bash
# Runs the Masroof worker, which ingests this month's bank/wallet SMS (SABB, Alinma,
# D360, STC Pay, tiqmo) from the macOS Messages database into the ledger on startup,
# then repeats weekly.
#
# Prerequisites:
#   1. SQL Server 2025 running (see deploy/compose or the masroof-sql-2025 container on :14333).
#   2. Ollama running locally with the parse model pulled.
#   3. Full Disk Access granted to the host app (System Settings > Privacy & Security >
#      Full Disk Access) so the process can read ~/Library/Messages/chat.db.
#
# To test against a fixture instead of the live inbox, export SmsInbox__DatabasePath
# to a sample chat.db before running.
set -euo pipefail

export PATH="/usr/local/share/dotnet/x64:$PATH"
cd "$(dirname "$0")/.."

export DOTNET_ENVIRONMENT="${DOTNET_ENVIRONMENT:-Development}"
export ConnectionStrings__Sql="${ConnectionStrings__Sql:-Server=localhost,14333;Database=Masroof;User Id=sa;Password=Masroof_pass123;TrustServerCertificate=True}"
export Llm__Endpoint="${Llm__Endpoint:-http://localhost:11434}"
export Llm__ParseModel="${Llm__ParseModel:-llama3.1:8b}"
export Llm__AskModel="${Llm__AskModel:-llama3.1:8b}"
export SmsIngestion__Enabled="${SmsIngestion__Enabled:-true}"
export SmsIngestion__RunOnStartup="${SmsIngestion__RunOnStartup:-true}"

echo "Starting Masroof worker (SMS ingestion for the current month)…"
exec dotnet run --project src/Masroof.Worker
