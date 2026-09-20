# Shared storage contract

This is the implementation handoff for plan.md, not another product specification.
Root owns the two shared C# contract files. Changes to this seam return to root
before a dependent lane uses them. C# signatures are frozen in T002 before lanes
start. Runtime types may use Unity/game types; domain types must compile without
game assemblies.

Convergence round 1 freezes the additional native-effect seam:
`IStorageEffectHandler.CaptureState(operationId, descriptor, escrow)` returns an
opaque before-state. The runtime must persist this with a Started receipt before
Apply. `Reconcile(operationId, descriptor, escrow, beforeState)` returns
`StorageEffectRecovery.NotApplied`, `Applied`, or `Uncertain`. Only proven
NotApplied may be retried or rejected/refunded; an uncertain result must retain
the same operation/escrow and must not invoke Apply again. Reconciliation precedes
ordinary Validate on a resumed Started receipt because a successful native effect
may have made the old precondition false. Root owns implementations in Integration;
the backend specialist owns invocation/persistence and production protocol tests.

## Domain

Storage/Contracts.cs contains immutable-value item keys, stacks with opaque
round-trippable payloads, physical inventory snapshots (stable ID, revision,
width/height, slots), allocation results, operation IDs/status and request kinds.
Keys compare every property listed in plan.md, including sorted custom data.
Neither display names nor flattened list indexes identify a physical item.

The core planner accepts snapshots and intent, and returns exact before/after
layouts and accepted/remainder quantities. It never mutates caller snapshots.
Deposit and withdrawal allow explicit partial results; a recipe payment requires
the entire cost. Organize may move between members but cannot create capacity.
Plans carry expected revisions and preserve opaque payloads. Deterministic
ordering makes the unchanged second organization a no-op.

The transaction coordinator consumes validated plans through durable participant
and journal ports. Prepare has no inventory effects; a durable commit decision
precedes all effects. Applying an operation twice has the same result as once.
Unknown results remain pending. An abort is final only before any commit decision.
Post-decision recovery replays participant effects by operation ID, never replans
against a later inventory. The production runtime uses this same coordinator;
tests must not verify an unused model.

## Runtime facade

Storage/Runtime/StorageFacade.cs exposes one injected IStorageService implementation
and the following consumer operations. It owns no duplicate inventory or protocol.

- Query(context): combined view of accessible real inventories. Context includes
  actor, origin, radius, scope (Terminal, Crafting, Processor or Direct) and the
  terminal/producer identity where relevant. Results contain physical member IDs,
  finite slots, exact-key item rows with a detached display ItemData, revisions and
  unavailable/busy state. Query never claims ownership or opens a chest.
- SubmitDeposit(context, player, item, amount): exact selected player item into
  backing chests, with partial acceptance. Equipped/protected items are rejected
  for bulk submissions. Service owns player escrow and transfer, not the GUI.
- SubmitWithdraw(context, player, itemKey, amount): backing chests into finite
  player capacity, preserving payload and partial acceptance.
- SubmitOrganize(context): compact only the terminal's members.
- PrepareCost(context, requirements, operationKey): obtain an all-or-nothing
  cost in accounted escrow, optionally including the player's own contribution.
  Requirements distinguish vanilla recipe name/quality matching from exact-key
  terminal withdrawal. The operation key identifies one craft/input attempt.
- SubmitOutput(context, producedItem, amount, operationKey): capture produced
  output in durable producer escrow before suppressing the native drop. Return a
  handle only after capture is recorded. Failure to capture means the caller
  retains its normal original production path. Delivery may accept partially.

The concrete CaptureOutput signature includes separate persisted captureEffect
and remainderEffect descriptors. The capture handler validates the actual native
production event/state and receipts its source transition before outbox delivery.
The remainder handler runs only after receiving-chest effects are confirmed.
Machine native state, output sequence, source receipt and pending outbox share the
producer ZDO/save unit; uncertain source capture suppresses duplicate production
while recovery settles the same ID. A distinct new production event can use a new
sequence only after the previous source event was receipted. Already-debited
native events (such as smelter Spawn after RemoveOneOre) are explicitly recognized
by the production adapter, not inferred from a client-provided quantity.
- GetOperation(id)/Resume(id): distinguish Preparing, Committed, RecoveryPending,
  Confirmed and Rejected; report authoritative accepted and remaining amounts.
  Handles survive window closure. A new request with an existing key resumes.
- CompletePrepared(id, effectDescriptor): complete a prepared payment using a
  registered idempotent consumer handler after context/capacity revalidation.
  Consumed resources cannot be exposed as normal usable inventory before the
  effect is settled. If revalidation rejects, return or preserve the escrow.
- SetNetworkName(target, actor, name), GetNetworkName(target), IsBusy(target):
  authenticated metadata changes with the same conflict rules as transfers.

An effect descriptor identifies a craft/player or machine/kind and captured
parameters. Root registers crafting and processor handlers before requests are
accepted. Runtime persists the descriptor and operation ID; a transient Action
delegate alone is not recoverable. Handlers validate, apply and reconcile using
the participant's operation receipt. Local native machine methods execute on the
current owner; do not enqueue an unreceipted second RPC as the final effect.
Receipt and resulting inventory/machine state are persisted in the same local
game save unit. A native exception with uncertain effects is RecoveryPending,
never an automatic refund. Client/profile versus world rollback remains the
documented Vanilla limitation, not a claimed cross-file transaction.

Craft descriptors also persist the chosen requirement and real quality for
single-ingredient recipes, plus required and extra output amounts and multiplier.
Player.GetFirstRequiredItem must return the selected projection sample for display
and the actual prepared escrow item during replay. CountItems success must never
lead to Inventory.GetItem returning null for a network-only ingredient. The
native quality-dependent Recipe.GetAmount calculation receives that exact item.

## Persistent game boundary

Versioned SCS ZDO metadata stores member labels and active participant intent,
prepared layout/escrow, decision and receipt references. Player custom data stores
its escrow/receipts. The coordinator journal is world-scoped, survives controlled
server restart and cannot mix operation IDs between worlds. Active entries are
never evicted; acknowledged completed receipts may be compacted only after every
participant can reject an old duplicate using retained monotonic sequence data.

Authenticated server RPCs coordinate current participant owners. Clients cannot
choose another actor, replace arbitrary inventory snapshots, or commit an
unvalidated context. Prepare/commit messages identify the expected owner/revision;
ownership changes trigger reconciliation, not a fresh withdrawal. Original
payloads are serialized with game inventory serialization. Stack placement must
not use a vanilla AddItem overload that merges metadata-distinct items.

During preparation/effects, ordinary container open/take-all/destruction/name
changes, all SCS writers and supported MultiUserChest mutation requests respect
the reservation. An already-open or unsupported conflicting member is unavailable
before preparation. Guard both authority and effects, not just the requesting
client's cached busy flag. Direct SCS features retain their existing discovery
scope but submit/guard writes through the same participant coordination.

## Independent lanes

Backend owns the implementation of these domain/runtime contracts and its tests.
UI owns new piece/GUI files and consumes only the frozen facade. Root owns all
existing-feature adapters and shared contract/project/config/package wiring.
Each lane can compile against the contract before runtime implementations exist;
the facade's uninitialized service reports unavailable rather than optimistic
success. Integration is complete only when Plugin initializes the real service
and all adapters use it. No stub implementation satisfies a requirement.

## Test boundaries

Run a net10 offline executable harness with production pure core sources. Use
deterministic external persistence/transport/time ports for lost responses,
duplicate/reordered delivery, owner changes, permission changes and restart.
Check literal expected inventory quantities, capacity and payload identities;
do not assert internal method-call counts. Runtime adapter checks must exercise
real decisions, not regex assertions of source text. Root's production
Storage/Integration/StationOperationController.cs is Unity-independent and is
included in this executable harness. Actual Harmony craft/processor adapters must
invoke these tested controllers for their payment, replay, duplicate-tick,
capacity and output-disposition decisions. External native callbacks are test
boundaries; verifying the sequence of visible paid resources and produced results
is behavioral, not an internal call-count test. Include network-only
single-ingredient selection/quality, native result-before-consumption ordering,
changed input capacity, repeated ticks, unknown acknowledgements, 8-of-10 output,
and every supported producer's remainder policy. Final independent review checks
all game adapters consume the tested decision path. Compile against local net48
game assemblies. Live visual/two-client scenarios remain separately reported.
