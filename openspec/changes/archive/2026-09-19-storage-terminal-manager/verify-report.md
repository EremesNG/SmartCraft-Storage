# Verification Report: Managed storage terminal

**Reviewer**: oracle<br>
**Independent from implementer**: Yes<br>
**Verdict**: PASS

Independent reviewer: `/root/oracle_terminal_verify_round3` (fresh round 3).
Root persists the reviewer's terminal verdict and evidence here. Production was
frozen during review. Prior FAIL reports remain in verify-report.round1.md and
verify-report.round2.md; their candidates are superseded.

## Review dimensions

- **Completeness**: PASS — all accepted FR and buildable SC are represented and checked. SC-008 and SC-009 remain explicit outcome risks.
- **Correctness**: PASS — no deterministic V3-xx finding remains. Persisted quantities, globally bounded discovery and host/remote captured-output recovery resolve V2-01 through V2-03.
- **Coherence**: PASS — contracts, runtime adapters, focused tests, operating instructions and the inspected five-entry package agree. Automated and inspection evidence are distinguished from unobserved gameplay.

## Compliance matrix

| Requirement | Implementation evidence | Executed check | Result |
| --- | --- | --- | --- |
| FR-001 | Storage/UI/TerminalRegistration.cs:13 and Storage/UI/StorageTerminalUi.cs:90 provide a buildable manager, pooled search, finite capacity, transfers, Organize and pending status. | Release build and independent piece/UI/facade inspection; live presentation remains SC-008. | PASS |
| FR-002 | Storage/Runtime/StorageDiscovery.cs:17 implements normalized names, range, loaded state and eligible physical backing filters. | Discovery-policy harness scenarios and independent runtime linkage inspection. | PASS |
| FR-003 | StorageAccess, StorageAuthorityPolicy.cs:21 and StorageRpc.cs:257 validate authenticated actors, wards, owners and reserved anchors again at commit. | Authority/reservation scenarios and independent owner/prepare/commit inspection. | PASS |
| FR-004 | StorageDiscoveryPolicy.CompleteProjection and StorageDiscovery.Complete deduplicate the complete physical union; exact identities stay distinct. | Duplicate projection, metadata identity and final-union-bound scenarios. | PASS |
| FR-005 | StoragePlanner, DepositSelection and player effects allocate finite compatible slots, protect equipped items and report confirmed acceptance. | Partial deposit, protected selection, payload identity and persisted 10/8/2 status scenarios. | PASS |
| FR-006 | StoragePlanner and player effect handlers limit withdrawals to real player capacity and preserve remainders. | Partial withdrawal and exact accepted/remaining status scenarios; runtime effect inspection. | PASS |
| FR-007 | StoragePlanner compacts mixed materials deterministically; UI sorting only changes presentation. | Four ten-slot members become three occupied members; repeated Organize produces zero moves; UI sort inspection. | PASS |
| FR-008 | Storage/Integration/CraftingStoragePatch.cs:41 stages the native result after complete durable payment; hammer remains direct. | Complete-payment, insufficient-cost and selected-ingredient scenarios; independent native craft/upgrade/escrow inspection. | PASS |
| FR-009 | ProcessorStorage and station adapters combine fresh queue/slot/fuel capacity with paid native effects for smelter, kiln, cooking, fireplace and fermenter. | Changed-capacity, input/fuel and kiln restriction scenarios; all adapters and cached native handlers inspected. | PASS |
| FR-010 | ProcessorOutput uses common CaptureOutput custody for smelter/kiln, cooking, fermenter and hive output, with exact remainders and require-all honey. | 8-of-10 remainder, uncertain capture and fresh-capacity require-all scenarios; native producer paths inspected. | PASS |
| FR-011 | Planner, transaction records, escrow and source outbox retain exact identity and finite capacity, including uncertain custody. | Conservation, identity, capacity reservation and unknown-response scenarios; runtime custody inspection. | PASS |
| FR-012 | Reservations, mutation guards, native effect receipts and payment-before-effect share coordinated resource accounting. | Competing reservations, payment gating and native reconciliation scenarios; direct/native guards inspected. | PASS |
| FR-013 | Monotonic IDs, incremental journal, replay filter, persisted intents, status reconstruction and effect receipts support reconnect and lost acknowledgements. | Duplicate/lost/reordered/restart/rejection/recovery scenarios and persisted public status; journal/RPC linkage inspected. | PASS |
| FR-014 | StorageDiscovery and station consumers operate independently of the terminal window and use loaded-world scope. | Independent closed-window discovery/consumer inspection and loaded-scope policy scenarios. | PASS |
| FR-015 | DirectPlayerTransfers and existing feature adapters retain direct quick-stack, restock, hammer, animal, plant and optional-integration reach. | Direct-scope regression scenarios and independent consumer/guard inspection. | PASS |
| FR-016 | Plugin.cs:9 enforces matching installations; synchronized configuration, authenticated server peers, anchor scope, current ZDO revision and owner validation govern effects. | Release build, authority-policy scenarios and deployment/protocol inspection. | PASS |
| FR-017 | Complete union bounds, pending admission, fair tick scheduling and explicit oversized/malformed replies preserve bounded work and distinct status. | Global-bound and status scenarios; server error replies, incremental persistence and retry scheduling inspected. | PASS |
| FR-018 | Plugin, focused harness, scripts/check-patches.ps1, five-entry package and operational/two-client README are coherent. | Independently rerun 32/32 tests, Release build, 28 metadata targets, diff check and package/hash inspection. | PASS |
| SC-001 [buildable] | Production discovery/authority policies deduplicate members and exclude ineligible members across the complete union. | Deduplication, exclusion, authority and global-union-bound scenarios plus runtime linkage inspection. | PASS |
| SC-002 [buildable] | Transfer planners and StorageTransactions.GetStatus preserve exact quantities and payloads. | Partial deposit/withdrawal and persisted requested 10, accepted 8, remaining 2 scenarios. | PASS |
| SC-003 [buildable] | StoragePlanner.Organize consolidates compatible stacks into finite physical members. | Four members compact to three occupied members; repeated unchanged operation makes zero moves. | PASS |
| SC-004 [buildable] | Crafting integration gates staged native output on complete payment and keeps hammer mode direct. | Complete-payment and insufficient-payment scenarios; inspected native craft staging and direct hammer path. | PASS |
| SC-005 [buildable] | All supported processor adapters use paid inputs and durable output custody with producer-specific remainder policies. | Changing capacity, kiln caps, 8-of-10 output, require-all honey and capture-order scenarios; every native adapter inspected. | PASS |
| SC-006 [buildable] | Transactions, native receipts, StorageCaptureFlow and immutable StorageRequestIntent provide stable replay/recovery and fresh planning inputs. | Duplicate/lost/reordered/restart/reject/recovery and immutable-intent/fresh-snapshot scenarios; journal/RPC ownership linkage inspected. | PASS |
| SC-007 [buildable] | Reservations, access/membership revalidation and direct writer guards prevent competing resource or slot spending. | Competing reservation, changed authority and direct-boundary scenarios plus native mutation-guard inspection. | PASS |
| SC-008 [outcome] | R3-SC008: no live construction, visual UI or complete player workflow was observed. | Live Valheim acceptance in README remains unexecuted. | RISK |
| SC-009 [outcome] | R3-SC009: no matching dedicated server/two-client interruption or ownership-handoff session was observed. | Live multiplayer acceptance in README remains unexecuted. | RISK |

## Findings

No deterministic V3-xx findings. No open CRITICAL issues.

Round-2 remediation confirmed by the independent reviewer:

- V2-01 RESOLVED: StorageTransactions.GetStatus reconstructs persisted requested,
  accepted and remaining quantities; runtime GetOperation uses that projection.
- V2-02 RESOLVED: the complete deduplicated union is bounded, client prechecks
  reject oversized requests and server parse/catch paths reply explicitly.
- V2-03 RESOLVED: intent, source outbox, escrow and active marker precede native
  mutation; uncertain custody remains captured. Immutable intent regenerates
  current snapshots and discovery. Capture reconciles before delivery, and
  require-all output can resume once actual capacity is available.

## Executed evidence

Independent Oracle executed or inspected the frozen round-3 candidate:

- `dotnet run --project tests/Storage.Contracts.Tests --no-restore`: PASS 32/32.
  The harness exercises production pure seams; it does not run the Unity scene.
- `dotnet build SmartCraftStorage.csproj -c Release --no-restore -p:ValheimManagedDir=C:/Users/EremesNG/AppData/Local/Temp/SCS-game-references-20260919`:
  PASS, 0 errors. A full compilation has the three known CS0436 publicizer
  warnings; incremental compilation can report zero warnings.
- `./scripts/check-patches.ps1 -ValheimManagedDir C:/Users/EremesNG/AppData/Local/Temp/SCS-game-references-20260919`:
  PASS, 28 explicit Harmony targets and their named parameters. Dynamic guards
  and native runtime call paths were inspected separately.
- `git diff --check`: PASS, line-ending notices only.
- Package inspected without regeneration: exactly plugins/SmartCraftStorage.dll,
  CHANGELOG.md, icon.png, manifest.json and README.md; no game assemblies or journals.
- `dist/SmartCraftStorage-0.7.0.zip` SHA256:
  `DC944728D5496FA7EAD2E0CFE94532E87993037887742DABF83ED9BCCB4CF551`.
- Packaged DLL matches the freshly built DLL, SHA256:
  `AC5F32078113CCCEFACAC31B2BA365016493FA9D83A80035269574B6E5782268`.
- Cached native cooking, smelter, fermenter, hive, inventory, ZDO and drop flows
  inspected. No network access or live game was used.

## Residual risks

- SC-008: R3-SC008 — visual layout, input focus, localization, construction and the complete terminal/station workflow require the live acceptance steps in README. Build, metadata and pure-seam checks do not establish these outcomes.
- SC-009: R3-SC009 — real transport timing, two clients, reconnect, owner handoff and optional MultiUserChest interaction require the documented live multiplayer acceptance session. Deterministic protocol checks do not establish these outcomes.

## Next action

No blocking open questions. Persist PASS, complete evidenced tasks and archive
all declared durable deltas while retaining SC-008/SC-009 as explicit RISK. The
reviewed package is ready for in-game acceptance; deployment and publication
were not performed.
