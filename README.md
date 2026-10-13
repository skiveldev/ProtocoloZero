# DESCARGAR PROTOCOLO ZERO PARA WINDOWS

**Descarga pendiente de publicación y cierre de las condiciones de distribución.** No hay un enlace operativo al juego en esta entrega. Consulte el [estado de entrega](DELIVERY_STATUS.md); este repositorio contiene únicamente código seleccionado y documentación, no un juego ejecutable.

## Protocolo Zero — Zona de Cuarentena

Proyecto académico de exploración y supervivencia: reunir tres tarjetas, activar el panel de seguridad, abrir la salida y alcanzar la zona de victoria mientras se evita un zombi. El rifle es solamente visual; no hay disparos ni munición.

### Ejecución cuando exista un paquete autorizado

1. Obtener el ZIP de Windows únicamente del enlace que publique el responsable después de verificar las condiciones pendientes. No descargar un supuesto ejecutable desde esta rama de código.
2. Descomprimir **todo** el ZIP en una carpeta; conservar `ProtocoloZero_Data`, las DLL y los demás archivos y carpetas acompañantes. No ejecutar desde el ZIP ni trasladar solamente el `.exe`.
3. Abrir `ProtocoloZero.exe` desde la carpeta descomprimida. Esta instrucción es condicional: no confirma que el paquete ya esté disponible.

Plataforma prevista: **Windows de 64 bits**, teclado y ratón. Editor del proyecto: **Unity 6000.5.10f1**. No se han establecido mínimos de CPU, RAM, GPU, versión concreta de Windows ni espacio descomprimido mediante una prueba de compatibilidad; no se garantiza funcionamiento en cualquier equipo de 64 bits.

| Control | Acción prevista |
|---|---|
| WASD | Movimiento |
| Shift | Correr |
| Ratón | Cámara |
| E | Interactuar |
| Jugar / Reiniciar | Iniciar / reiniciar mediante la interfaz |

Estos controles proceden de la documentación existente; no representan una nueva prueba de juego. La aprobación previa del usuario y los resultados históricos 84/84 no son verificaciones nuevas.

### Entrega académica alternativa

El código seleccionado y la documentación están disponibles para revisión académica, sin runtime ni recursos de terceros: [rama chore/public-source](https://github.com/skiveldev/ProtocoloZero/tree/chore/public-source). GitHub ofrece un [archivo ZIP de la rama — SOLO CÓDIGO, no el juego](https://github.com/skiveldev/ProtocoloZero/archive/refs/heads/chore/public-source.zip), cuya disponibilidad se confirmó mediante HTTP 302 seguido de 200 en el cierre de entrega. Esta alternativa puede utilizarse para la evaluación del código, pero no es un sustituto ejecutable del proyecto completo ni concede una licencia de reutilización.

La entrega privada de un compilado a docentes sigue condicionada a los mismos derechos de distribución; no constituye una excepción automática. Créditos, fuentes oficiales y exclusiones: [dependencias](LICENSE-DEPENDENCIES.md). Condiciones del runtime: [estado de entrega](DELIVERY_STATUS.md).

## Source snapshot details

**Source-only snapshot, not playable out of the box.** This independent repository preserves team-provided source from commit `05928590c2a42f2b62f6b15be5998ce46641e6f2`. The owner reports that the original game and Windows build are functional and accepted; that does not establish playability of this asset-free snapshot.

## What is included

39 C# files, 11 assembly definitions, their 50 matching metadata files, 15 ancestor folder metadata files, Git ignore/attribute rules, package manifests and the Unity editor version: 120 unchanged original blobs. The two-commit baseline contains those blobs plus this README and the [dependency and licensing record](LICENSE-DEPENDENCIES.md): 122 files. The delivery-status document is an additional documentation file; the local ODD tracker is ignored by Git and is not part of that tracked baseline.

Scenes, prefabs, controllers, materials, models, textures, sounds, six Mixamo animation clips, navigation data, builds and all project settings except `ProjectVersion.txt` are deliberately absent. Source references are not bundled resources. Even conservatively classified team-authored cube/controller assets are excluded; exclusion is not a finding that they are unlawful.

## Gameplay represented by the source

| System | Original intended setup |
|---|---|
| Player | WASD movement, Shift sprint, mouse camera; E interaction |
| Objective | Collect three cards, activate the security panel, open the exit and reach the victory zone |
| Health | 100 health; zombie proximity damage of 20, one-second cooldown, 1.4-unit range |
| Enemy | One zombie; detection 8, loss 12, decision interval 0.25 seconds; patrol/chase speeds 1.5/2.5 |
| Interface | Menu, health, cards, objective, victory/defeat screens; Jugar and Reiniciar buttons |
| Feedback | Audio and damage-flash code; required external references must be supplied |
| Rifle | Visual attachment only: no shooting, ammunition, combat system or zombie health |

These values describe source defaults and original scene-validation expectations, not a supplied scene or a newly tested public game.

## Manual reconstruction

1. Create a fresh Unity **6000.5.10f1** Universal Render Pipeline project. Preserve the versions in the supplied manifests when installing packages through Unity. Key versions are URP **17.5.0**, Input System **1.20.0**, AI Navigation **2.0.14**, uGUI **2.5.0** and Test Framework **1.7.0**. No packages were installed during this export.
2. Import the scripts, assembly definitions and their metadata without changing their identities. Acquire any desired external assets separately through the official sources in the dependency record, under your own applicable license. No private assets are supplied here.
3. Author a level and configure the URP pipeline/settings manually. Assemble player, camera, colliders, interaction targets, three cards, panel, gate and victory zone. Wire `GameFlow` to `PlayerHealth`, `LevelProgress`, its `controlledBehaviours` and feedback references; wire each other component's serialized references explicitly.
4. Configure Humanoid avatars, disable root motion, enable looping on locomotion clips and create Idle/Walk/Run animation states or a corresponding blend tree using the `Speed` parameter. Player animation uses actual movement velocity; zombie animation uses actual NavMeshAgent velocity. Supply the missing rigs, bodies, clips and controllers yourself.
5. Create and bake navigation for the authored scene, assign the zombie agent, patrol targets, navigation bootstrap and gate obstacle references, and configure the intended gameplay values above.

**Exact reconstruction is not automatic.** The original main scene, authored warehouse layout, controllers and prefabs are excluded. Editor animation/weapon setup utilities expect specific external asset paths and an already authored scene; they do not generate the original warehouse. Their existing code is preserved, not adapted into a substitute playable project.

The Windows build helper requires `Assets/_Game/Scenes/ProtocoloZero_Nivel01.unity`, its dependencies and necessary URP/navigation data. It will not work until a suitable project has been reconstructed or the appropriately licensed private project restored. No Windows executable or companion files are included or moved.

## Verification and boundaries

The strict selection manifest SHA256 is `2c0db1c23c861ff2e514fa23cd5bfe86c1070c7e16567696747547e9b5a8d72a`. Exported files were compared byte-for-byte with the pinned Git blobs, without line-ending conversion. No LFS media or pointer blobs were exported; `.gitattributes` is retained for future explicitly authorized use only.

Own-source tests are retained, but asset-dependent tests require excluded scenes/resources. No tests, PlayMode sessions or builds were run. Historical original-project results, including 84/84, are not evidence that this snapshot passes. Two local original-project settings changes were not exported and remain untouched.

## Ownership and publication decisions

Team authorship is owner-supplied provenance, not an independent copyright determination. The user explicitly authorized publication of the selected own code, and that baseline is now published. No downstream open-source license has been selected; this informational distinction does not block the authorized publication. Public visibility does not automatically grant reuse rights; no MIT/GPL grant or invented contributor attribution is added.

The initial publication of `chore/public-source` was confirmed through GitHub API at `ba06fa071dd6bf476d132cac4cb506a29d4711f8`; subsequent documentation commits remain on this separate branch. Its parentless root `5ecc3e87796ec58e315114316a5abcb189631ed6` records the 122-file snapshot. Public annotated tag `v1.0.0-pa4` (`847d4b3a6b386f133aace17b6840819e36f54794`) points to that root, not to the latest branch documentation. This unrelated history is not a normal mergeable PR into existing `master`; do not merge, rebase or cherry-pick original history into it. The Windows game release remains pending.

An independent clean branch does **not** remove restricted material from existing public history. The owner must separately coordinate permissions and repository visibility/history decisions. A new clean public repository with appropriately licensed original material preserved privately is one possible owner decision; retaining the existing URL may require separately authorized owner-managed renaming/visibility changes. Archiving alone does not hide public contents. Nothing here authorizes overwriting, renaming, deleting or rewriting the existing repository.
