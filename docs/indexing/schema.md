# Indexing Schema Draft (IDX-01/IDX-02)

> Purpose: Map file explorer nodes and media metadata into a DB-backed index
> so that the UI, queue, and sessions can run DB-first.

## Decisions Captured (must-follow)

- **DB-first** lookups; filesystem fallback only via folder indexing when missing
  and no full scan is running.
- **Manual indexing**: full scan or folder refresh only.
- **Incremental updates**: preserve IDs to keep playback sessions/queues stable.
- **FTS5** used for name search.
- **Root removal** archived, not deleted (user can purge).

## Core Entities

### FileIndexEntry (proposed)

| Field | Type | Notes |
| --- | --- | --- |
| Id | Guid | Primary key. Stable across refresh. |
| RootChildId | Guid | FK to root child node entity. |
| RelativePath | string | Relative path from root child. Unique with RootChildId. |
| Name | string | File/folder name (for display + search). |
| IsFolder | bool | Folder vs file. |
| CreatedUtc | DateTime | From file system. |
| ModifiedUtc | DateTime | From file system. |
| Size | long? | Null for folders. |
| MimeType | string | From existing MIME resolver. |
| IsMedia | bool | From existing file metadata logic. |
| MediaBitrate | int? | From TagLib (MediaInfo). |
| MediaDuration | TimeSpan? | From TagLib (MediaInfo). |
| MediaTracklistJson | string? | Serialized `ImportTracklistDto`. |
| ArchivedAtUtc | DateTime? | Set when root removed. |
| DeletedAtUtc | DateTime? | Set when file removed on refresh. |
| LastIndexedUtc | DateTime | When indexer last updated this row. |

### FileIndexSearch (FTS5)

- Virtual table for fast name-based search.
- Columns: `Name`, `RelativePath` (optional), `RootChildId` (stored or filterable).
- Sync strategy: manual upsert during indexer writes.

## Metadata Mapping

Media metadata must be captured from the existing TagLib-based update flow:

- `MediaBitrate`, `MediaDuration`, `MediaTracklistJson` come from `MediaInfo`
  created by `UpdateMediaMetadataCommandHandler` (TagLib + tracklist service).
- `MimeType`/`IsMedia` sourced via existing MIME resolution and metadata converter.

## Constraints

- Unique constraint on `(RootChildId, RelativePath)` to preserve identity.
- Refresh updates **in place** (no delete+reinsert) so playback sessions and
  queues keep stable IDs.

## Refresh Behavior (incremental)

**Folder refresh** should perform a diff between filesystem and index:

1. **Existing rows**: update metadata fields in place.
2. **New files**: insert new rows.
3. **Missing files**: mark `DeletedAtUtc` (and optionally remove from search).

**Full scan** repeats the same diff logic for all roots.

## Queue/Playback Implications

- Queue and session should resolve files directly from index rows.
- If a playback session references a missing index entry, trigger folder refresh.

## Open Questions (confirm with owner)

- Tracklist storage: JSON blob vs normalized tables.
- Case normalization for `RelativePath` (for cross-platform stability).
