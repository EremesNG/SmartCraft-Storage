# NET-001: bounded storage recovery traffic (0.7.8)

## Observed regression

The user reproduced `Failed to send data k_EResultLimitExceeded` after joining
with matching 0.7.7 on client/server, before using storage. With the previous
local-merge backup on both ends, the error did not occur. Earlier read-only
diagnosis found two durable processor cost intents in the client profile and
5,548 send-limit log entries. Those messages are socket send failures, not
managed exception traces or a count of distinct application requests.

The service retried pending operations from every Update and explicit Resume,
including rebuilding full player/member snapshots. Native socket retries of an
already queued failed packet can amplify the visible log spam. The A/B result
and reproduced application flood strongly implicate storage-net; real Steam
acceptance of the corrected candidate is still required.

## Correction and boundaries

- Root owns the retry schedule, runtime service/RPC wiring and isolated fixture.
  The bounded implementation specialist could not start (native thread limit).
- One monotonic schedule per operation gates all client intent and server resume
  entry points: initial attempt, then 0.5/1/2/4/8-second delays, capped at eight.
  Authenticated new participant progress wakes its operation; duplicate receipts
  do not. Session changes discard transient timing/admission, retaining durable
  pending work.
- Once admitted, requests poll status. A server missing the intent or needing
  current output discovery requests fresh data using the existing Requested
  status. This control reply preserves the previous public status, quantities
  and captured-output custody, and does not restart the backoff.
- Retryable commands defer when the actual routed socket is disconnected or
  already queues at least 64 KiB. Results, acknowledgements and one-shot effect
  releases keep their existing path. Local RPC remains synchronous.
- Existing wire names, journal/profile encodings, permissions, item identities,
  UI, shortcuts and crafting/processor behavior remain the compatibility boundary.
  No installed game, config, server or player/world save was changed.

## Test evidence

The fixture executes the production DLL in actual Valheim/Unity/BepInEx, using
real routed RPC serialization, peer objects and inventories. Socket and clock
are external controlled boundaries. The native game startup is suppressed and
the process has its own temporary savedir/config; there is no Steam connection.

- `network-red.log`: old service sends 402 packets for two restored operations
  during 100 immediate Tick/Resume iterations. Delaying retries reduces this to
  two; see `network-retry-partial.log`.
- The early backpressure fixture lacked the native Connected connection status.
  Its no-send failures are retained in `network-backpressure-fixture-failure.log`
  as fixture failures, not evidence for the corrected behavior. Only the early
  clock/flood assertions are qualified; final backpressure checks use Connected.
- `network-admission-red.log` / `network-admission-green.log`: admitted operations
  switch from repeating complete intent to status requests.
- `network-custody-red.log`: fresh-intent replies initially cleared the client's
  captured-output flag/quantities; the final handler preserves them.
- `network-polling-red.log`: resetting on alternating Requested/RecoveryPending
  replies reproduced 3,600 sends in 60 seconds. A fresh-intent probe now preserves
  public state/backoff, yielding 11 attempts over the same interval.
- `network-final-green.log`: **33/33**, including 30/60/144-FPS cadence, pending
  custody, spoofed/duplicate acknowledgements, final outcomes, reconnect,
  client/server backpressure, ownership and synchronous local dispatch.
- Existing contracts: **40/40**; runtime adapter/journal checks: **4/4**.
- Release: zero errors, three known CS0436 publicizer warnings. Metadata:
  **39 Harmony targets/arguments and 5 Unity lifecycle signatures**.
- Full ready validator valid; the pre-existing conditional checklist warning
  remains unactivated. `git diff --check` passes.

Simplify review retained one shared retry gate and one fresh-output-intent
predicate, with explicit custody comments and no unrelated refactoring.

## Combined worktree evidence

The implementation was committed as dc50935 on storage-net and merged into
local-merge as 4c4c757. Only CHANGELOG.md conflicted; its local cart/overlay notes
and the feature's 0.7.8 entry were both retained. The combined candidate passes
33 network checks, 15 cart checks, 18 actual-Unity overlay checks, and the 39/5
Harmony/lifecycle check. The source contributes 40 contract and 4 runtime checks:
110 focused assertions in total. Prior 27 UI/config assertions were not rerun
because those files are unchanged, and are not counted in that total.

Both source and combined 0.7.8 ZIPs were built, with only the expected five files.
The final documentation-only follow-up records this evidence. Rebuilds after the
audit merge update only build commit metadata; the tested production code stays
the same. Final hashes, exact commit IDs, clean-worktree checks and ZIP/DLL match
are recorded in the combined worktree's dist/local-merge/BUILD-INFO.txt.

Prior 0.7.7 packages remain available. No cart/overlay changes were merged back
into storage-net, which remains suitable as the future upstream feature branch.

## Remaining acceptance / capability gap

Fresh `oracle_storage_network_078_final` returned `agent thread limit reached`;
no independent reviewer was created and no Oracle PASS is claimed. CAP-001,
T014 and archive T015 remain open. T024 tracks integration/package evidence
separately from that missing judgment. The root's executable checks are evidence,
not independent approval.

Live acceptance requires matching 0.7.8 on client/server: join idle, processor
input/output, terminal deposit/withdraw/organize, reconnect, and concurrent owners.
Observe quantities and recovery as well as the absence of send-limit spam. The
isolated test does not establish real Steam behavior or full-game UI acceptance.
