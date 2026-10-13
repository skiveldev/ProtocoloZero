# Estado de entrega PA4

**Código seleccionado disponible en la rama pública; descarga del compilado pendiente.** Este documento no otorga licencias, no acepta contratos en nombre del responsable y no acredita cumplimiento del ZIP mediante la mera inclusión de enlaces.

## Qué se puede entregar

- Código seleccionado y documentación base disponibles para evaluación académica, sin runtime ni recursos de terceros: [rama pública](https://github.com/skiveldev/ProtocoloZero/tree/chore/public-source), [README](README.md) y [dependencias](LICENSE-DEPENDENCIES.md). La publicación de la rama fue verificada por el operador principal.
- [Archivo de GitHub de la rama — SOLO CÓDIGO](https://github.com/skiveldev/ProtocoloZero/archive/refs/heads/chore/public-source.zip): no es el juego ni incluye el runtime. El operador confirmó su disponibilidad mediante HTTP 302 seguido de 200 y tipo `application/zip`, sin ejecutar el contenido.
- El compilado, tanto público como entregado de forma restringida a docentes, requiere cerrar las condiciones siguientes. Una entrega académica privada no elude contratos de distribución.

## Versiones y fuentes del runtime

Las versiones son evidencia del investigador previo sobre el paquete existente, no una inspección binaria repetida en esta unidad. Solo se relacionan los componentes identificados; no se incorporan avisos de SDK o componentes ausentes.

| Componente identificado | Versión observada | Fuente oficial y estado |
|---|---|---|
| UnityPlayer | 6000.5.10f1, revisión 3bd4f66ad299; PE 6000.5.10.3921142 | [API oficial de versión](https://services.api.unity.com/unity/editor/release/v1/releases?version=6000.5.10f1) y [corpus oficial Windows Mono Player](https://storage.googleapis.com/live-platform-resources-prd/templates/assets/Player_Windows_Mono_6000_5_10f1_a62c405a91/Player_Windows_Mono_6000_5_10f1_a62c405a91.pdf): 36 páginas de esta versión/revisión, conservadas como PDF original en la nueva copia de trabajo |
| DirectML.dll y DirectML.Debug.dll | 1.13.1; 240111-2204.1.dml-1.13.9b0890f | [Licencia binaria 1.13.1](https://www.nuget.org/packages/Microsoft.AI.DirectML/1.13.1/License); [capa de depuración](https://learn.microsoft.com/en-us/windows/ai/directml/dml-debug-layer). Evidencia previa permite integración en juegos Windows/Xbox y documenta ambas DLL en el redistribuible |
| dstorage.dll y dstoragecore.dll | PE 1.3.2506.2501; producto 1.3.0 | [Acuerdo 1.3.0](https://www.nuget.org/packages/Microsoft.Direct3D.DirectStorage/1.3.0/License); [archivo oficial exacto](https://api.nuget.org/v3-flatcontainer/microsoft.direct3d.directstorage/1.3.0/microsoft.direct3d.directstorage.1.3.0.nupkg): ambas DLL figuran en distributable_files.txt; LICENSE.txt binario y NOTICES.txt completo conservados |
| D3D12Core.dll y d3d12SDKLayers.dll | 1.618.1.0.20250923.2 | [Acuerdo 1.618.1](https://www.nuget.org/packages/Microsoft.Direct3D.D3D12/1.618.1/License); [archivo oficial exacto](https://api.nuget.org/v3-flatcontainer/microsoft.direct3d.d3d12/1.618.1/microsoft.direct3d.d3d12.1.618.1.nupkg): ambas DLL, incluida SDKLayers, figuran en distributable files.txt, junto con d3dconfig.exe; LICENSE.txt binario conservado; sin miembro separado de avisos de terceros identificado en ese archivo |
| PIX / WinPixEventRuntime | PE 1.0.2403.08001; producto 1.0.240308001 | [MIT específico de versión](https://www.nuget.org/packages/WinPixEventRuntime/1.0.240308001/License), copyright (c) Microsoft Corporation; texto completo incorporado al ZIP actualizado del borrador |
| Mono nativo y ensamblados identificados | Nativos sin versión PE; Mono.Security 4.0.0.0; mscorlib 4.6.57.0 | Corpus oficial Windows Mono Player de la fila UnityPlayer: Mono Unity Fork en página 9 y MIT completo en página 31; copyrights Ximian 2001–2003, Novell 2003–2010 y Xamarin 2012. Resuelve la fuente de atribución exacta; no se sustituye por una licencia genérica de otra revisión |

Newtonsoft: aviso ya resuelto en el paquete previo; no se repite su auditoría. Los manifiestos Unity del código son referencias de dependencias, no copias redistribuidas de sus paquetes. Las atribuciones de recursos visuales/animaciones están en el documento de dependencias; los recursos originales siguen excluidos de esta rama.

## Condiciones exactas que debe resolver el responsable

| Responsable | Evidencia o decisión pendiente antes de entregar el compilado |
|---|---|
| Responsable de distribución, con proveedor Unity si hace falta | El corpus exacto Unity/Mono ya está localizado y conservado. Acreditar la aplicabilidad y cumplimiento de condiciones del corpus: CPOL 1.02, sección 5e (acuerdo de destinatarios) y sección 7 (indemnidad), notificación del código triangle-strip y TetGen bajo acuerdo negociado de 2012. Los acuerdos previos de Unity pueden cubrir el runtime; no se presume notificación incumplida ni necesidad de un nuevo acuerdo TetGen independiente |
| Responsable contractual autorizado | Implementar y documentar un mecanismo real que requiera a usuarios externos y distribuidores aceptar términos al menos tan protectores para Microsoft/software como ambos acuerdos. Publicar un README o aviso no demuestra asentimiento. La funcionalidad primaria significativa está respaldada por el gameplay propio documentado: movimiento, tarjetas, panel, salida y victoria; no se exige una nueva prueba del juego |
| Responsable contractual autorizado | Decidir explícitamente, con asesoramiento si corresponde, sobre las obligaciones de indemnizar, defender y mantener indemne a Microsoft de ambos acuerdos. Esta unidad no las acepta por el usuario ni sustituye esa decisión |
| Responsable del paquete | El ZIP con avisos y su checksum ya sustituyeron los dos adjuntos DRAFT bajo autorización expresa. GitHub confirmó sus digests y la continuidad del borrador. No autoriza publicación ni entrega privada. DirectML no se reabre; añadir avisos no acredita el cierre contractual |

La publicación del código propio seleccionado está autorizada por el usuario y ya realizada. La licencia de reutilización permanece sin elegir; no es un bloqueo adicional a esa publicación. Los permisos generales de uso integrado Asset Store/Mixamo están registrados en dependencias, sin repetir la auditoría de adquisiciones. La evidencia de otras obligaciones no resueltas no permite afirmar incumplimiento categórico ni ausencia de avisos adicionales.

La ausencia de un aviso localizado **no equivale a una prohibición**. Tampoco se ha establecido una prohibición general sobre DLL de depuración: no retirar DirectML.Debug ni d3d12SDKLayers arbitrariamente.

El asentimiento protector exigido por DirectStorage/D3D12 es una condición decisiva, no necesariamente la única pendiente. No se infiere un formato obligatorio de clic de aceptación. Las etiquetas MIT del corpus Unity para componentes Microsoft no reemplazan los acuerdos binarios NuGet exactos; LICENSE-CODE.txt para cabeceras tampoco concede por sí solo distribución de las DLL.

## Seguimiento acotado y conservación del paquete

La evidencia nueva sustituye las carencias anteriores sobre el corpus exacto Unity/Mono y las listas nominales Microsoft: se conservaron el PDF oficial y los miembros legales originales de los dos archivos NuGet exactos. Las cuatro DLL enviadas están expresamente listadas; no se añade d3dconfig.exe, cabeceras ni muestras ausentes. Esta unidad leyó ambas listas completas sin repetir la investigación de versiones/hashes de DLL ni explorar recursos o código.

El ZIP original y su checksum se conservan sin modificaciones: 387742442 bytes y SHA-256 `a229b89ad31bb3c400e513285f0d0e31d099489561643351130eecf92b4ded9a`; CHECKSUMS tiene 96 bytes. Su verificación previa no se repite. La comparación histórica de la copia preparada conservó los 215 archivos: 214 idénticos y ThirdPartyNotices.txt ampliado con el prefijo original intacto; ningún byte del runtime cambió. No se repitió la auditoría de Newtonsoft.

El **nuevo ZIP con avisos, PENDIENTE DE ACEPTACIÓN**, ya está adjunto a la release `v1.0.0-pa4` en borrador: **387908266 bytes, 224 archivos**, SHA-256 `e0dfa1407943d88e2e667b479f00b51bb765b1dbb2f3aa38f998d6e0bd1ec50a`. Incluye `Legal/Unity/`, `Legal/DirectStorage/`, `Legal/D3D12/`, `Legal/PIX/` e índices de fuentes/hashes. Solo se empaquetó la carpeta del juego, sin la evidencia externa. Verificación: 224/224 CRC correctos, coincidencia con la carpeta preparada y digests de GitHub iguales a los locales. `CHECKSUMS.sha256` tiene 96 bytes y digest `e0e8e8d6792d7282715bb2e675c4b7e0a999d3cc5439987ccb3ac39c976103cd`. Los nuevos assets son ZIP `638350763` y checksum `638350761`; la release continúa `draft=true`, `published_at=null`. No existe una descarga pública operativa del videojuego. La integridad no resuelve contratos: el responsable debe seguir [las instrucciones de aceptación personal](PUBLICATION_ACCEPTANCE.md) y aportar hechos y evidencia completos antes de publicación o entrega privada.

## Límite de publicación

La base de publicación consta de dos commits independientes, con raíz `5ecc3e87796ec58e315114316a5abcb189631ed6` y límite previo `ba06fa071dd6bf476d132cac4cb506a29d4711f8`; las actualizaciones documentales se añaden sin alterar esos commits. No corresponde un PR hacia `master`: las historias no tienen el mismo origen y esta entrega no autoriza su integración ni reescritura.

El verificador independiente revisó los 124 blobs únicos alcanzables y confirmó que los 120 originales coinciden con Git `05928590c2a42f2b62f6b15be5998ce46641e6f2`, sin recursos originales excluidos ni indicadores de credenciales o rutas privadas; los enlaces documentales relativos existen. Es evidencia aportada, no una investigación repetida por este escritor, ni una garantía universal de ausencia de secretos. El operador principal confirmó que los metadatos del commit raíz ya eran públicos; la rama no introduce una nueva divulgación de identidad ni exige un consentimiento adicional.

El operador principal confirmó el push inicial de `chore/public-source` y la rama mediante GitHub API en `ba06fa071dd6bf476d132cac4cb506a29d4711f8`. La URL de la rama ya es operativa. La etiqueta pública anotada `v1.0.0-pa4`, objeto `847d4b3a6b386f133aace17b6840819e36f54794`, apunta a la raíz `5ecc3e87796ec58e315114316a5abcb189631ed6`; las actualizaciones documentales no cambian esa referencia.

La rama limpia no elimina recursos de la historia pública anterior. El registro histórico de dependencias identifica material Warehouse, Character08 y 3Rifle ya publicado; sus antiguos recuentos de refs no describen el estado remoto actual. El operador principal ya reconcilió la release draft con la etiqueta; su publicación y un enlace operativo de descarga del juego siguen pendientes por las condiciones de runtime.

No se ejecutaron tests Unity, Play Mode, builds ni el juego. La prueba previa del usuario y 84/84 son antecedentes, no verificaciones de esta entrega documental.
