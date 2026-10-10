# Phase 03 — third-person camera

## Objective and scope
Add a behind-and-slightly-above Renegade third-person mouse orbit with smooth follow, camera-relative WASD and bounded wall avoidance. Preserve Phase 02 movement speeds, CharacterController collision shape, root-motion setting, disabled demo, and existing active MainCamera/AudioListener. No player movement script edits, packages, or project settings.

## Route and preparation
- Single delegated writer on clean master at a934005; RDD globally off, no review, stage, commit or publish authorization.
- Existing camera GameObject 963194225, transform 963194228, Camera 963194227, listener 963194226, disabled legacy look 963194229; Renegade instance 791183685, GameObject 791183686, controller 791183687 and movement 791183688. Inspect the prefab source transform before wiring the target.
- Deliver one coherent behavior/test/scene/doc unit. Forecast ~300–400 authored lines plus a scene backup; 400 is advisory, ask-on-risk rather than compressing tests or code.
- Save a byte-identical scene backup with independent meta GUID before modifying the scene. Use only authorized paths. Unity EditMode runner output stays in the approved external temp directory.

## Checklist
- [x] P03-01 Back up the original scene and verify equal SHA256 and distinct meta GUID.
- [x] P03-02 Add focused deterministic EditMode camera tests and record observed compile RED before implementation; no behavior RED occurred before implementation.
- [x] P03-03 Implement Input System orbit, cursor ownership, independent yaw/pitch, smooth following and bounded collision avoidance with self/trigger filtering.
- [x] P03-04 Wire MainCamera and Renegade movement reference in the existing scene without changing Phase 02 behavior.
- [x] P03-05 Run authorized EditMode suite, inspect test XML/log and verify scene GUID linkage, backup and clean source boundaries. Parent corrected the scene script GUID and independently verified the corrected linkage after the final test run.
- [x] P03-06 Record results, remaining manual Play Mode checks and no-commit status here and in the Engram task mirror.
- [x] P03-07 User confirmed Phase 03 was tested in the level and works well; manual acceptance is user-reported, not an assistant Play Mode run.

## Acceptance checks
- At Play startup camera snaps behind the player's initial facing at configurable height/distance and follows smoothly in LateUpdate; mouse pixels rotate yaw and clamped pitch while cursor is locked, Escape releases and explicit click recaptures. Null target or mouse does not throw.
- Camera orientation remains independent of player facing, and assigned MainCamera movement reference makes existing WASD relative to horizontal camera axes without altering speed/collision code.
- Geometry between pivot and camera retracts distance immediately with a sphere-cast/overlap bounded fallback; self colliders and triggers do not occlude, and distance recovers smoothly when clear. Impossible enclosed geometry may force a very close position rather than guarantee an unobstructed view.
- Five existing movement tests and new focused camera tests pass. Manual actual-level framing, cursor behavior and varied corner geometry remain Play Mode checks; EditMode tests cannot establish these visually.

## Verification record
- Final acceptance: user confirmed "fase 03 comprobado, quedo bien" after the pending-check handoff. Phase 03 is accepted with ten passing EditMode tests and user-reported Play Mode validation. Earlier pending statements below describe the pre-acceptance state. No new commit or phase is authorized by this confirmation.
- Original scene backup SHA256 `E1E63A96AE3FFDBD1F9EA3917F3CF0BE7F9DD25836B66258A9507DDA3F3B36CB` matched the original Phase 03 scene before edits; unique backup meta GUID `f038a6e4f3bd4b458ba27e6fd470a201`. Parent independent verification found the initially authored scene script GUID unresolved; parent corrected only its `m_Script` reference to actual `OrbitCameraController.cs.meta` GUID `fd3be2568061b004fae30607f7c449c4` and confirmed the corrected scene reference persisted after the final Unity run. Transform source ID `2602126525003700056` from Renegade prefab GUID `bc0db8f04b753bd49b7f06ae6ab40513`, stripped scene transform `791183689` and controller `963194230` are linked. MainCamera Camera/AudioListener remain active and old look disabled; movement speed fields and CC shape unchanged. Git diff confirms existing PlayerMovement.cs and PlayerMovementTests.cs bytes unchanged.
- Unity RED invocation before implementation aborted with missing `OrbitCameraController` compiler errors: compile RED only, not a behavior-level failure. First post-implementation run discovered 9 tests, 7 passed/2 failed: mouse EditMode cursor locking was unavailable and physics transforms required explicit sync in test. Second run: 9 total, 8 passed/1 failed because Euler x wraps to 325 degrees for -35; corrected assertion uses DeltaAngle. Final authorized EditMode command produced 10/10 passed, 0 failed, 0 skipped (5 camera and 5 unchanged movement). Mouse delta/clamp are tested via private method with InputTestFixture device; runtime lock/Escape/click require manual Play Mode validation.
- Parent final batch result `C:\Users\Skivel\AppData\Local\Temp\opencode\phase03-final-results.xml` reports 10 total, 10 passed, 0 failed, 0 skipped (five camera and five existing movement tests). This confirms EditMode tests after the GUID correction, not actual-level runtime acceptance. Unity again removed `SENTIS_ANALYTICS_ENABLED` from Standalone scripting symbols in `ProjectSettings/ProjectSettings.asset`; parent restored the exact original afterward. Current git status/diff show no ProjectSettings change, and no settings behavior change was authorized. Untracked `.atl/` appeared from tooling and was left untouched outside Phase 03. No commits/stage/review performed. Manual visual framing, corner geometry, cursor release/recapture and camera-relative motion in the real scene remain pending.
