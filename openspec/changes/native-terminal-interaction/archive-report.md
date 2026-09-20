# Archive Report: Native terminal interaction and reliable linking

**Status**: deferred; change remains active

T014 has no independent verdict because the required fresh Oracle could not be
created (native agent thread limit reached). Therefore T015 is not executed,
no requirement deltas are synchronized to openspec/specs, and this change is not
moved to the archive. The previous 0.7.0 audit trail remains intact.

The 0.7.8 candidate adds bounded network recovery to the previous 0.7.7 fixes.
Fresh oracle_storage_network_078_final again could not start because of the
native thread limit. The user's matching-version server acceptance is pending;
the pre-storage rollback comparison is diagnostic evidence, not a 0.7.8 PASS.

The preceding 0.7.7 local test package (including native scrollbar baseline
initialization, adaptive occupied-entry cells, native network weight, configurable
Alt+N shortcut, search input gate and reliable toolbar clicks) and focused evidence are recorded in
verify-report.md. Resume with a fresh Oracle and the user's local game results;
repair any findings, verify the resulting candidate, then archive only on PASS.
