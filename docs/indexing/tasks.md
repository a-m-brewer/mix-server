# Indexing Task Board

> Rule: pick the lowest Priority with Status=TODO and dependencies DONE.

| ID | Priority | Status | Owner | Summary | Dependencies | Links/Notes |
| --- | ---: | --- | --- | --- | --- | --- |
| IDX-01 | 1 | TODO |  | Map metadata fields (MediaInfo + File metadata) into index schema. | — | Confirm TagLib/MediaInfo mapping. |
| IDX-02 | 2 | TODO |  | Define DB schema + FTS5 migration plan. | IDX-01 | Include archive/delete flags + LastIndexedUtc. |
| IDX-03 | 3 | TODO |  | Manual indexer service (full scan + folder refresh) with scan state guard. | IDX-02 | Enforce single-scan-at-a-time. |
| IDX-04 | 4 | TODO |  | DB-first FileService + on-demand folder indexing flow. | IDX-03 | Missing entry -> folder refresh unless full scan running. |
| IDX-05 | 5 | TODO |  | Queue/playback DB-native changes (no filesystem hydration). | IDX-04 | Session lookup from index rows. |
| IDX-06 | 6 | TODO |  | Search API (FTS5 query + response DTO). | IDX-02 | Name-based search across roots. |
| IDX-07 | 7 | TODO |  | Archive/purge policy for root changes. | IDX-02 | Roots archived, user can purge. |
| IDX-08 | 8 | TODO |  | Test/validation plan. | IDX-04 | Cover indexing diff + session/queue regression. |

## Status Definitions

- TODO: Not started.
- IN_PROGRESS: Actively being worked.
- DONE: Completed and merged.
- BLOCKED: Waiting on a dependency or decision.
