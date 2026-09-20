# Verification Report: Managed storage terminal

**Reviewer**: `/root/oracle_terminal_verification` (round 1)<br>
**Independent from implementer**: Yes<br>
**Verdict**: FAIL

## Review dimensions

- **Completeness**: FAIL. Production recovery, remote-server authority and bounded work do not cover the accepted scope (V1-01 through V1-06).
- **Correctness**: FAIL. Passing pure contracts do not establish the production transport, persistence or native-effect boundaries (V1-01 through V1-06).
- **Coherence**: FAIL. Recovery/boundedness documentation and the default verification command exceed or contradict implementation evidence (V1-07).

Oracle's exact conclusion: **FAIL**. Completeness, correctness, and coherence do not meet the accepted Full SDD scope. The pure contract model passes, but production runtime boundaries violate FR-003, FR-005/006, FR-008–014, FR-016–018 and SC-002/004–007.

## Compliance matrix

| Requirement | Implementation evidence | Executed check | Result |
| --- | --- | --- | --- |
| FR-001 | Terminal/UI exists; delayed results lose quantities | Runtime review V1-03 | FAIL |
| FR-002 | StorageDiscovery.cs:17-107 normalized/ranged/loaded membership | Contract executable and code review | PASS |
| FR-003 | Terminal anchor absent from reservations | Runtime review V1-05 | FAIL |
| FR-004 | Deduplication and exact identity projection | Contract executable | PASS |
| FR-005, FR-006 | Planner conserves; production resume loses counts | Contract executable + V1-03 | FAIL |
| FR-007 | Compaction and idempotence | Contract executable | PASS |
| FR-008, FR-009, FR-010 | Adapters wired; remote execution/effects unsafe | V1-01, V1-03, V1-04 | FAIL |
| FR-011, FR-012 | Unknown effects can replay/refund; capture unjournaled | V1-04 | FAIL |
| FR-013 | IDs, resume, persistence, receipt compaction | V1-02 | FAIL |
| FR-014 | Server depends on remote local-scene objects | V1-01 | FAIL |
| FR-015 | Direct feature boundaries retained | Contract executable + code review | PASS |
| FR-016 | Client context insufficiently validated | V1-01, V1-05 | FAIL |
| FR-017 | Unbounded history and unreliable status | V1-03, V1-06 | FAIL |
| FR-018 | Build/package present; docs/command mismatch | V1-07 | FAIL |
| SC-001 [buildable] | Discovery/deduplication | Contract executable | PASS |
| SC-002 [buildable] | Production delayed results are zero quantities | V1-03 | FAIL |
| SC-003 [buildable] | Four-chest compaction and idempotence | Contract executable | PASS |
| SC-004 [buildable] | Payment confirmation precedes native effect | V1-03 | FAIL |
| SC-005 [buildable] | Uncertain native capture/input not recoverable | V1-04 | FAIL |
| SC-006 [buildable] | Production protocol does not implement tested model | V1-01, V1-02, V1-04 | FAIL |
| SC-007 [buildable] | Anchor/authority paths unsafe | V1-05 | FAIL |
| SC-008 [outcome] | No live Valheim UI acceptance observed | N/A | RISK |
| SC-009 [outcome] | No live two-client acceptance observed | N/A | RISK |

## Findings

| ID | Severity | Dimension | Evidence | Remediation anchor |
| --- | --- | --- | --- | --- |
| V1-01 | Critical | Correctness/deployment | StorageService.AuthenticatedActor uses Player.GetPlayer; server discovery uses local Physics; participant recovery uses ZNetScene.FindInstance. Native cached game code limits each to instantiated local-scene objects. | Server authority must use peer identity/world ZDOs plus validated owner snapshots/operations, without remote Unity objects. |
| V1-02 | Critical | Correctness/recovery | Journal assumes a ZoneSystem ZNetView; NewId resets its in-memory sequence; reconnect loses request envelopes; player receipts use unsafe Take(256). | Verified persistent world boundary, durable/collision-proof identities, status/resume RPC, durable requests and safe replay rejection. |
| V1-03 | High | Correctness/status | ResumeTransaction publishes StorageTransactions.ToOperation without quantities; payment confirmation is sent before native effect settles. | Persist result quantities and publish the outer operation only after required effect completion. |
| V1-04 | High | Correctness/conservation | Unknown capture returns before durable effect/active marker; native mutation precedes receipt and can replay or refund after exception. | Persist intent before mutation; operation-specific reconciliation for native effects; retain producer sequence and escrow under uncertainty. |
| V1-05 | High | Authority/access | Server accepts radius up to 128 and client-selected descriptors; terminal anchor not reserved/revalidated. | Validate configured scope/station/proximity/actor at authority boundary, reserve terminal anchors and validate membership before commit. |
| V1-06 | High | Completeness/performance | Journal rewrites retained full payloads; completed histories/maps unbounded; MaxPending ignores recovered durable work; truncated receipts replayable. | Acknowledged bounded compaction, replay rejection metadata, durable pending counts and bounded per-tick work. |
| V1-07 | Medium | Coherence/tooling | README reconnect/boundedness claims contradicted; check-patches.ps1 default PluginAssembly fails with empty PSScriptRoot in parameter initializer. | Correct script default and align operating documentation with verified behavior. |

## Executed evidence

- Contract executable: PASS 18/18.
- net48 Release build with cached references: PASS, 0 warnings, 0 errors.
- Explicit `-PluginAssembly` patch metadata check: PASS 28 targets.
- Documented patch command without that override: FAIL.
- Root-reported packaging: PASS (not release approval); ZIP SHA-256 `94A5E8BADC779C773354F47D85AE39C2F161E3249A3395EFE29213776ADD8B86`.
- Harness compiles core/controller code, not the Unity runtime service/RPC/journal/adapters; extend actual production protocol coverage during convergence.

## Residual risks

- SC-008: In-game construction, naming, search, transfer, compaction, keyboard/input and panel acceptance remain unobserved.
- SC-009: Two remote clients, interrupted connection/ownership transition and optional MultiUserChest simultaneous access remain unobserved. Known runtime failures above must be fixed first.
- Actual shipped ZoneSystem scene components were not observed; this uncertainty does not change the verdict because other recovery and remote-scene faults are independently demonstrated.

## Next action

Converge on V1-01 through V1-07, extend tests through production protocol seams, then request a fresh independent Oracle. Do not archive this candidate.
