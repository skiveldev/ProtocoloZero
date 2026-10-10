# Phase 02 — player movement

## Objective and scope
Add an Input System-only, CharacterController-driven player movement component to the existing Renegade character. Keep the original demo assets intact. Disable the scene's first-person demo, detach its camera without moving it in world space, and disable legacy mouse-look while retaining the active Camera and AudioListener. No follow camera, animation, or jump behavior.

## Route and preparation
- Delegated implementation: single writer, exact allowed edit surfaces supplied by parent; RDD globally off, no review lifecycle.
- Existing scene evidence: Renegade prefab instance 791183685, CC 791183687 (radius 0.3, height 1.8, center 0/0.9/0); demo 1656206103; camera 963194225, transform 963194228, mouse-look 963194229.
- Unity 6000.5.10f1, Input System 1.20.0, Test Framework 1.7.0; initial repository is untracked, without baseline commit. No commit/stage/publish authorization.
- Delivery: one coherent behavior + tests + scene work unit; ask-on-risk if review size or integration risk warrants it. Forecast below 400 authored lines if readable; advisory only, not a reason to compress tests or code.

## Checklist
- [x] P02-01 Create an independent scene backup with unique meta GUID before scene changes.
- [x] P02-02 Add focused EditMode behavior tests and observe RED with the authorized Unity runner, or record the exact environment exception.
- [x] P02-03 Implement namespace-isolated input movement: WASD world axes, optional horizontal movement reference, Shift sprint, diagonal clamp, smooth yaw, grounded bias and gravity, no-keyboard safety.
- [x] P02-04 Integrate only the Renegade component and demo/camera scene overrides; verify GUID links, preserved world camera, CC shape and scale.
- [x] P02-05 Run authorized EditMode tests for GREEN and relevant alternate cases: final run passed all five tests.
- [x] P02-06 Record verification and remaining risk in this document and its Engram mirror; no commit.
- [x] P02-07 User confirmed actual-level Play Mode movement, wall collisions, Shift sprint and gradual facing rotation; screenshot confirms visible camera output. Detailed grounded/airborne edge cases were not separately tested.

## Acceptance checks
- The Renegade character moves by `CharacterController.Move` using Input System keyboard input; walk/run speeds are Inspector-configurable, diagonal speed is capped, and absence of a keyboard is safe. Facing turns smoothly toward movement; grounded state limits downward velocity and gravity applies in air.
- The optional movement reference defaults to null (world-axis input) and projects right/forward horizontally. No jump, animations, or camera follow are added.
- Renegade keeps unit scale and CC radius 0.3, height 1.8, center (0, 0.9, 0); scene root motion remains off. Demo GameObject is inactive; detached camera retains world transform and its Camera/AudioListener stay enabled, while mouse-look is disabled.
- Focused EditMode tests and scene linkage checks recorded with observed results, not inferred Play Mode coverage. Original asset scripts, BuildSettings, SampleScene, and input settings are untouched.

## Verification record
- Backup SHA256 before changes: `240E297EBFB22086C73937D27CD8C3A2F2C6DC60FB3783A2AAAD383589351CB9`, identical to initial scene. Backup meta GUID `42c239cedf8749d5a61cf654598ec4b4` differs from original `8c0e65f0ef7180246b64f68bc412cdd2`.
- RED attempt: initial Unity command exceeded 180 seconds. Its result XML reported 0 tests (not a valid RED); log reported invalid initial test .meta GUID, corrected afterward. Independent verification then identified a wrong InputTestFixture namespace and a test framework assembly incorrectly listed as a precompiled DLL. The installed Input System TestFramework asmdef names `Unity.InputSystem.TestFramework`; it is now an asmdef reference, with only `nunit.framework.dll` precompiled. The namespace import was removed (InputTestFixture lives in `UnityEngine.InputSystem`).
- Intermediate correction run: Unity compiled and discovered 5 tests; 4 passed and 1 failed. `DiagonalSpeedDoesNotExceedCardinalSpeed` expected 3 +/- 0.02, got 5.54327726. The failing test reused a falling CharacterController for the second step. The corrected test compares independent freshly constructed actors; its successful final rerun is recorded below. No Play Mode evidence is claimed.
- Scene YAML integration: 791183688 references movement script GUID `a12768bd5f3240ac9e304911462fd7f8` on stripped Renegade GameObject 791183686; instance 791183685 adds component. CC 791183687 shape remains unchanged and root motion override stays 0. Demo 1656206103 inactive; camera transform 963194228 reparented root, world position (12.46, 1.7, 3.36) and yaw -32.701; camera 963194227 and listener 963194226 stay enabled, mouse component 963194229 disabled. No added follow behavior.
- Scene SHA256 after integration `E1E63A96AE3FFDBD1F9EA3917F3CF0BE7F9DD25836B66258A9507DDA3F3B36CB`. No scene or asset source changes in the test correction. No commits or baseline; actual-level runtime acceptance remains pending.
- Parent spot check after the test correction: Unity batch EditMode run completed with `phase02-final-test-results.xml`: total 5, passed 5, failed 0, skipped 0. Observed GREEN for world forward, diagonal normalization, sprint speed, no keyboard and optional tilted reference. No valid pre-implementation RED was observed.
- Final functional status: Phase 02 accepted with five passing EditMode tests and user-reported Play Mode confirmation of WASD, wall collisions, Shift sprint and gradual rotation. Screenshot shows active camera output. Detailed grounded/airborne edge cases remain untested; animation and camera follow are out of scope.
