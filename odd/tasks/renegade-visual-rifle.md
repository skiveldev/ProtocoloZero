# Renegade visual rifle attachment

## Intent
Attach the existing Sniper_Rifle_01 prefab as a scene-only visual child of the original Renegade's Humanoid right hand. Retain the accepted Rifle Idle/Walk/Run controller, movement, camera, source character and weapon assets. No combat, IK, or runtime logic is authorized.

## Checklist
- [x] Validate source rifle prefab and scene Humanoid right hand without changing source assets; material YAML references URP asset version and shader GUID, but rendered compatibility is unverified.
- [x] Back up the exact scene bytes with a distinct GUID before scene edits.
- [x] Add and observe a failing scene-level EditMode attachment test.
- [x] Author only a right-hand RifleSocket and one weapon prefab instance; preserve all other scene data.
- [x] Run the complete focused EditMode suite and inspect the scene diff and incidental changes.
- [ ] Manually inspect grip, muzzle direction, visibility and shading in the Unity level; adjust only the socket transform with user visual feedback.

## Verification
Before editing, scene and backup SHA256 both `208AD6A2439B795321FD2E1CEA3FE8B4D926AC412F3055568C6BC8D057112675`; backup meta GUID `c92620d4dd3e4f8aa9e3d526746e4d0a`. RED Unity EditMode XML `visual-rifle-results.xml`: 17 passed, one failed because no RifleSocket exists. `WeaponVisualSetup.AttachRifle` completed successfully in `visual-rifle-authoring.log`. GREEN XML: 18 passed, zero failed/skipped. Scene addition: `RifleSocket` GameObject 788550828 / Transform 788550829, right-hand source transform 9003503333127757959, scene stripped transform 1185220785; socket local position `(0.02,0,0.06)`, Euler `(0,90,0)`, scale one. Prefab instance 1590232330 is child of socket with identity local pose and GUID `8e932e37952f66549b83c02c624baceb`. This pose is only a provisional inspector-adjustable guess; no Unity visual session was available. Rifle material retains source GUID `976c28b9735e83c4ca0f371653414029` and shader GUID `933532a4fcc9baf4fa0491de14d08ed7`; no material edits or pink-shader guarantee. Unity batch again changed out-of-scope `ProjectSettings/ProjectSettings.asset` (SENTIS flag); parent must handle, writer must not touch it. The original character importer meta had unrelated pre-existing changes. Engram task mirror and readback remain parent-owned because writer cannot read memory.
