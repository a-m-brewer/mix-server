# File Indexing Task Starter Pack

This folder contains the planning documents and coordination tools for the
DB-backed file indexing initiative.

## Purpose

- Capture **decisions** and **constraints** agreed for indexing.
- Provide **ready-to-implement** task guidance for agents.
- Keep schema and orchestration notes synchronized with implementation.

## Files

- `schema.md`: Detailed schema design + metadata mapping + FTS5 plan.
- `tasks.md`: Task board with dependencies and status.

## Operating Rules (must-follow)

1. **DB-first**: runtime lookups come from the index. If missing, index the
   current folder (unless a full scan is running).
2. **Manual indexing**: full scans and folder refreshes are user-triggered.
3. **Single scan at a time**: full scan blocks folder refresh.
4. **Incremental updates**: refresh updates existing rows in place to preserve
   IDs used by queues and playback sessions.
5. **Media metadata required**: `MediaInfo` fields must be stored in the index.
6. **FTS5 preferred**: name-based search uses SQLite FTS5.
7. **Root removal**: roots are archived; user can purge archived entries.

## How to Use

1. Assign IDX-01 and IDX-02 to agents first.
2. Agents update `tasks.md` with status, links, and notes.
3. Agents must **flesh out these docs as they implement** (treat the docs as
   living artifacts, not static plans).
4. Keep `schema.md` in sync with migrations and code changes.
