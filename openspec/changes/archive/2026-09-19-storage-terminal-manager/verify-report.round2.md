# Verification Report: Managed storage terminal

**Reviewer**: `/root/oracle_terminal_verify_round2` (round 2)<br>
**Independent from implementer**: Yes<br>
**Verdict**: FAIL

Round 1 is preserved in verify-report.round1.md. Production was frozen during this review.

## Review dimensions

- **Completeness: FAIL** — production host/remote recovery and globally bounded discovery remain incomplete.
- **Correctness: FAIL** — three deterministic defects violate persisted quantities, honest bounded status, and captured-output recovery.
- **Coherence: FAIL** — the 27/27 harness excludes StorageService, RPC, journal, and native adapters, while README recovery claims contradict production behavior.

## Findings

| ID | Severity | Evidence | Exact remediation anchor |
| --- | --- | --- | --- |
| V2-01 | High | StorageService.cs:204 reconstructs retained transactions without persisted requested/accepted/remaining quantities. Recovered final operations report 0 moved / 0 remaining. | Share reconstruction from StorageTransactionRecord.Requested/Accepted with Existing/ToOperation; test the public persisted-status boundary. |
| V2-02 | High | StorageDiscovery.cs:36 bounds each terminal, not the deduplicated union with direct chests. StorageService.cs:959 sends oversized sets, server throws at 1031, catch at 1094 logs without replying. | Bound the final projection; return explicit unavailable/rejected results for oversized or malformed requests. |
| V2-03 | Critical | Host CaptureOutputCore (StorageService.cs:180) applies the native capture before the outer journal record. Unknown returns Captured=false, allowing ProcessorOutput.cs:112 native fallback. Remote require-all stores context as literal remote (1233), cannot replan through RestoreContext, and retries stale snapshots. Honey can remain captured after freeing capacity. | Unify host/remote custody. Persist outer intent/outbox before suppression; retain uncertain custody; refresh authoritative context/current snapshots under the same operation identity. |

## Compliance matrix

| Contract | Result | Evidence |
| --- | --- | --- |
| FR-001 | FAIL | Controls exist; recovered quantities fail V2-01. |
| FR-002 | PASS | Named/ranged/loaded membership and backing filters inspected and exercised. |
| FR-003 | PASS | Peer/owner/ward checks and anchor reservations connected. |
| FR-004 | PASS | Physical deduplication and exact identities. |
| FR-005 | FAIL | Persisted partial deposit quantities fail V2-01. |
| FR-006 | FAIL | Persisted withdrawal quantities fail V2-01. |
| FR-007 | PASS | Mixed compaction and repeated no-op pass. |
| FR-008 | PASS | Native result staged, complete payment, direct hammer scope. |
| FR-009 | PASS | Input/fuel capacity validation and paid effects. |
| FR-010 | FAIL | Host uncertainty and remote honey recovery, V2-03. |
| FR-011 | FAIL | Inconsistent uncertain output custody, V2-03. |
| FR-012 | FAIL | Missing host outer intent before capture, V2-03. |
| FR-013 | FAIL | Persisted quantities and recoverable output, V2-01/V2-03. |
| FR-014 | PASS | Closed-window discovery with loaded-world scope. |
| FR-015 | PASS | Quick-stack/restock/building/animal/plant/optional integration remain direct. |
| FR-016 | PASS | Strict matching installations, synchronized settings and authenticated owner checks. |
| FR-017 | FAIL | Oversized request hangs, degraded status and unrecoverable require-all output, V2-01–03. |
| FR-018 | FAIL | Package/tests exist, docs exceed production recovery/boundedness. |
| SC-001 [buildable] | FAIL | Ordinary cases pass; combined bound missing, V2-02. |
| SC-002 [buildable] | FAIL | Planner passes, public recovered quantities fail, V2-01. |
| SC-003 [buildable] | PASS | Four members compact to three; repeat zero moves. |
| SC-004 [buildable] | PASS | Complete payment gates craft, insufficient cost produces no result, targets resolve. |
| SC-005 [buildable] | FAIL | Standard adapters pass; remote automatic honey recovery fails, V2-03. |
| SC-006 [buildable] | FAIL | Pure recovery passes; production host custody/stale replan fail, V2-03. |
| SC-007 [buildable] | PASS | Authority, reservation, conflict and direct-boundary checks pass. |
| SC-008 [outcome] | RISK R2-SC008 | No live UI/construction/full-workflow observation. |
| SC-009 [outcome] | RISK R2-SC009 | No live two-client/disconnect/ownership/MultiUserChest observation. |

## Executed evidence

- Harness PASS 27/27; it includes Core, StationOperationController and DepositSelection, not the failing Unity runtime surfaces.
- Fresh net48 rebuild PASS, 3 known CS0436 publicizer warnings, 0 errors.
- Patch metadata check PASS, 28 explicit targets/named parameters.
- Package PASS, five expected entries. Reviewer-regenerated ZIP SHA256 `F81F4E627ED009B329D97351125E716A39AE1B3BE81B33C53F992F06D2DE9256`.
- git diff --check PASS, line-ending notices only.
- Cached native crafting, processor, drop and ZDO decompiles inspected; no live game used.

## Residual risks

- **R2-SC008**: Visual layout, input focus, localization, construction and full in-game workflow unobserved.
- **R2-SC009**: Real transport timing, two clients, owner handoff and optional MultiUserChest concurrency unobserved. Known V2-02/V2-03 defects must be repaired first.

## Next action

Converge V2-01 through V2-03 with production-boundary regressions, then fresh independent Oracle. No open human-owned questions. Do not archive this candidate.
