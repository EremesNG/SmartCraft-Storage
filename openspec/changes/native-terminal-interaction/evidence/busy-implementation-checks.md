# BUSY-001: local chest and cooking reservations

Status: implementation and independent verification in progress; no corrected
live-game acceptance or installation is claimed.

## Qualified diagnosis

- User installed matching local-merge 0.7.8 and reports ordinary chests and
  cooking stations permanently busy immediately after local-world entry.
- Installed DLL SHA256 matches combined 0.7.8. Readonly current logs identify
  local Pruebas and do not show the earlier send-limit flood.
- `StorageService.ResumeTransaction` rebuilds participant wrappers on each
  attempt. Their old per-instance acknowledgement sets are discarded; native
  local output also takes that RPC path. Inline replies were followed by an
  unconditional Unknown result. Runtime/anchor and host-player acknowledgement
  routing had additional missing cases.
- A native service/RPC fixture with two real inventories and one captured-output
  source reproduces four failed completion/conservation/release assertions.
  See `busy-local-red.log`. A second qualified test reproduces an ignored
  durable apply receipt after reservation release: `busy-receipt-red.log`.

## Readonly custody evidence

Independent diagnostic Oracle `oracle_storage_busy_local_diagnosis` confirms
native v40+ `ZDO.Load` assigns new IDs. Operation IDs remain valid opaque tokens,
but saved raw participant/target/anchor references do not identify the same
object after load. The journal also permitted memory-only Save success when
its registered prefab/root was unavailable.

Native parsers consumed protected world .46/.47 copies exactly. Three sources
retain applied CapturePending receipts, matching capture intents and one cooked
meat outbox each. Three derived delivery-0 reservations have no apply receipts.
The .46 copy has no journal; .47 has one empty root and no records. The native
IDs changed across these saves; the opaque operation IDs did not.

A separately decoded protected character copy proves exact profile/world
custody agreement for all three operations. Pending-output bytes, profile
escrow bytes and native source outbox bytes match. Capture descriptor data,
persisted escrow-effect data and source capture-intent data match. World and
actor match. The profile has no effect-active key, which is not required because
the source world marker proves capture custody. Character snapshot SHA256:
`EBEECA4210A2CCC0A9F00D070EB7737DD7FED2D51902FA49850DF3C32B6CF4C4`.
Original profile hashes match before/after decoding; earlier hash changes were
external game saves. No original save, profile, game installation or config was
written. Private decoded payloads and copies remain in ignored diagnostic
directories and are not added to the repository.

## Current correction and checks

The RPC now keeps receipts across wrapper replacement, scoped by operation,
participant, actor, owner and native object. Only requested, authenticated,
new phase evidence reports progress. Owner/session changes invalidate transient
receipts. Host self replies are accepted. A durable apply receipt is acknowledged
without touching inventory or requiring an obsolete reservation. The existing
time/backpressure controls remain in place.

The initial qualified native network fixture passed 53 checks, including a
complete local output recovery, all three reservation releases, exact item
conservation, delayed rebind replies, interleaved operations, owner changes,
host-player phases and the earlier 0.7.8 traffic regressions. This executes the
actual game assemblies in an isolated Unity runtime, not a real Steam session.

The composed fixture subsequently passed 61 checks including journal gating,
first-use identity initialization, exact legacy orphan recovery, missing
capture-proof retention and actual native serialization of an owner snapshot
without the newer server marker. The marker survives deserialization; native
dictionary reservation grows capacity and does not delete absent keys.

Independent final round 1 nevertheless returned FAIL for interrupted cleanup,
unanchored legacy actor claims and missing-trailer reference fallback. T028/T029
are being implemented on the separate service surface. T030 has qualified
red/green evidence: legacy root and separate record loads used the wrong current
object before the fix and now preserve unresolved references without stamping
that object. Runtime checks pass 7/7. T028/T029 qualified reds reproduce incorrect
stored actor acceptance, interrupted cleanup and missing future source authority.
The composed correction passes 67 native checks with durable host profile proof,
restart-safe cleanup and a bounded operation/world/actor authority field. A
receipt on an already released old participant blocks a second delivery. Remote
legacy claims without independently verified actor custody stay pending.

Simplify review retained one phase-receipt state and one capture-authority encoder;
the new authority shares the current capture-intent lifetime and adds no dynamic
per-output key. Receipt authentication, timing, conservation and presentation
contracts remain unchanged by the review. Broader cleanup was deferred.

See the canonical verification report for exact independent findings. Combined
tests, a fresh Oracle verdict and matching 0.7.9 packages remain pending; no
live-game acceptance is claimed.

Independent round 2: `oracle_storage_busy_079_round2` returns PASS for the source
and closes all three findings with no blocker. Canonical verify-report.md records
the complete matrix and residual live risks. Combined delivery checks are next.
