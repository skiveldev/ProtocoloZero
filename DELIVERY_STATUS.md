# Estado de entrega PA4

**Código seleccionado disponible en la rama pública; descarga del compilado pendiente.** Este documento no otorga licencias, no acepta contratos en nombre del responsable y no acredita cumplimiento del ZIP mediante la mera inclusión de enlaces.

## Qué se puede entregar

- Código seleccionado y documentación base disponibles para evaluación académica, sin runtime ni recursos de terceros: [rama pública](https://github.com/skiveldev/ProtocoloZero/tree/chore/public-source), [README](README.md) y [dependencias](LICENSE-DEPENDENCIES.md). La publicación de la rama fue verificada por el operador principal.
- [Archivo de GitHub de la rama — SOLO CÓDIGO](https://github.com/skiveldev/ProtocoloZero/archive/refs/heads/chore/public-source.zip): no es el juego ni incluye el runtime. No se afirma una verificación HTTP de ese archivo en esta unidad.
- El compilado, tanto público como entregado de forma restringida a docentes, requiere cerrar las condiciones siguientes. Una entrega académica privada no elude contratos de distribución.

## Versiones y fuentes del runtime

Las versiones son evidencia del investigador previo sobre el paquete existente, no una inspección binaria repetida en esta unidad. Solo se relacionan los componentes identificados; no se incorporan avisos de SDK o componentes ausentes.

| Componente identificado | Versión observada | Fuente oficial y estado |
|---|---|---|
| UnityPlayer | 6000.5.10f1, revisión 3bd4f66ad299; PE 6000.5.10.3921142 | [Manual 6000.5](https://docs.unity3d.com/6000.5/Documentation/Manual/LicenseOverview.html): trata activación/licencias del editor, no proporciona el corpus de avisos del Player de esta revisión |
| DirectML.dll y DirectML.Debug.dll | 1.13.1; 240111-2204.1.dml-1.13.9b0890f | [Licencia binaria 1.13.1](https://www.nuget.org/packages/Microsoft.AI.DirectML/1.13.1/License); [capa de depuración](https://learn.microsoft.com/en-us/windows/ai/directml/dml-debug-layer). Evidencia previa permite integración en juegos Windows/Xbox y documenta ambas DLL en el redistribuible |
| dstorage.dll y dstoragecore.dll | PE 1.3.2506.2501; producto 1.3.0 | [Acuerdo 1.3.0](https://www.nuget.org/packages/Microsoft.Direct3D.DirectStorage/1.3.0/License); [paquete oficial](https://www.nuget.org/packages/Microsoft.Direct3D.DirectStorage/1.3.0): LICENSE.txt cubre native/bin/, no native/include/ |
| D3D12Core.dll y d3d12SDKLayers.dll | 1.618.1.0.20250923.2 | [Acuerdo 1.618.1](https://www.nuget.org/packages/Microsoft.Direct3D.D3D12/1.618.1/License); [paquete oficial](https://www.nuget.org/packages/Microsoft.Direct3D.D3D12/1.618.1): LICENSE.txt cubre build/native/bin/, no build/native/include/ |
| PIX / WinPixEventRuntime | PE 1.0.2403.08001; producto 1.0.240308001 | [MIT específico de versión](https://www.nuget.org/packages/WinPixEventRuntime/1.0.240308001/License), copyright (c) Microsoft Corporation; conservar el texto completo aplicable en una futura distribución |
| Mono nativo y ensamblados identificados | Nativos sin versión PE; Mono.Security 4.0.0.0; mscorlib 4.6.57.0 | [Unity Mono unity-main/LICENSE](https://github.com/Unity-Technologies/mono/blob/unity-main/LICENSE): MIT/BSD mixto, no identifica por sí solo la revisión ni todos los avisos correspondientes a los binarios enviados |

Newtonsoft: aviso ya resuelto en el paquete previo; no se repite su auditoría. Los manifiestos Unity del código son referencias de dependencias, no copias redistribuidas de sus paquetes. Las atribuciones de recursos visuales/animaciones están en el documento de dependencias; los recursos originales siguen excluidos de esta rama.

## Condiciones exactas que debe resolver el responsable

| Responsable | Evidencia o decisión pendiente antes de entregar el compilado |
|---|---|
| Responsable de distribución, con proveedor Unity si hace falta | Obtener el corpus de avisos de UnityPlayer 6000.5.10f1 / 3bd4f66ad299 y mapear la revisión Mono enviada a sus copyrights y textos MIT/BSD completos; conservar los avisos aplicables, no copiar indiscriminadamente los de otra revisión |
| Responsable de distribución | Obtener las listas de archivos redistribuibles y los ThirdPartyNotices aplicables de los paquetes exactos DirectStorage 1.3.0 y D3D12 1.618.1; confrontarlas con las DLL enviadas. Las páginas consultadas identifican directorios cubiertos, pero no enumeran esas listas ni el corpus de terceros |
| Responsable contractual autorizado | Implementar y documentar un mecanismo real que requiera a usuarios externos y distribuidores aceptar términos al menos tan protectores para Microsoft/software como ambos acuerdos. Publicar un README o aviso no demuestra asentimiento. La funcionalidad primaria significativa está respaldada por el gameplay propio documentado: movimiento, tarjetas, panel, salida y victoria; no se exige una nueva prueba del juego |
| Responsable contractual autorizado | Decidir explícitamente, con asesoramiento si corresponde, sobre las obligaciones de indemnizar, defender y mantener indemne a Microsoft de ambos acuerdos. Esta unidad no las acepta por el usuario ni sustituye esa decisión |
| Responsable del paquete | Incorporar el aviso PIX MIT completo, todavía ausente del ZIP conservado, y preservar los avisos Microsoft y textos aplicables. DirectML tiene permiso documentado de uso integrado Windows/Xbox; no se afirma que el corpus de avisos adicionales esté completo. Esta documentación no modifica el ZIP |

La publicación del código propio seleccionado está autorizada por el usuario y ya realizada. La licencia de reutilización permanece sin elegir; no es un bloqueo adicional a esa publicación. Los permisos generales de uso integrado Asset Store/Mixamo están registrados en dependencias, sin repetir la auditoría de adquisiciones. La evidencia de otras obligaciones no resueltas no permite afirmar incumplimiento categórico ni ausencia de avisos adicionales.

La ausencia de un aviso localizado **no equivale a una prohibición**. Tampoco se ha establecido una prohibición general sobre DLL de depuración: no retirar DirectML.Debug ni d3d12SDKLayers arbitrariamente.

## Seguimiento acotado y conservación del paquete

Se consultó una única ruta nueva de Unity: el Manual de licencias 6000.5. Responde con información de activación del editor, no con avisos del runtime ni un mapa de Mono para 6000.5.10f1. Se consultaron las páginas oficiales de los dos paquetes NuGet exactos y sus acuerdos binarios; no expusieron la lista nominal redistribuible ni ThirdPartyNotices. La investigación se cierra aquí sin repetir búsquedas locales ni descargas de ejecutables.

El ZIP y su checksum se conservan sin modificaciones por esta unidad. Verificación nueva aportada por el operador principal y el verificador independiente: ZIP de 387742442 bytes, 215/215 entradas con CRC correcto, sin rutas de escape, duplicados ni recursos originales; SHA-256 `a229b89ad31bb3c400e513285f0d0e31d099489561643351130eecf92b4ded9a`. CHECKSUMS tiene 96 bytes y coincide exactamente en nombre de archivo y digest. El escritor no repitió esas comprobaciones ni inspeccionó Newtonsoft. La integridad no resuelve los contratos o avisos pendientes. Recomendación: mantener el compilado pendiente; una futura preparación de avisos o paquete requiere cierre de condiciones, autorización y nueva verificación de integridad. No eliminar DLL para sortear condiciones.

## Límite de publicación

La base de publicación consta de dos commits independientes, con raíz `5ecc3e87796ec58e315114316a5abcb189631ed6` y límite previo `ba06fa071dd6bf476d132cac4cb506a29d4711f8`; las actualizaciones documentales se añaden sin alterar esos commits. No corresponde un PR hacia `master`: las historias no tienen el mismo origen y esta entrega no autoriza su integración ni reescritura.

El verificador independiente revisó los 124 blobs únicos alcanzables y confirmó que los 120 originales coinciden con Git `05928590c2a42f2b62f6b15be5998ce46641e6f2`, sin recursos originales excluidos ni indicadores de credenciales o rutas privadas; los enlaces documentales relativos existen. Es evidencia aportada, no una investigación repetida por este escritor, ni una garantía universal de ausencia de secretos. El operador principal confirmó que los metadatos del commit raíz ya eran públicos; la rama no introduce una nueva divulgación de identidad ni exige un consentimiento adicional.

El operador principal confirmó el push inicial de `chore/public-source` y la rama mediante GitHub API en `ba06fa071dd6bf476d132cac4cb506a29d4711f8`. La URL de la rama ya es operativa. La etiqueta pública anotada `v1.0.0-pa4`, objeto `847d4b3a6b386f133aace17b6840819e36f54794`, apunta a la raíz `5ecc3e87796ec58e315114316a5abcb189631ed6`; las actualizaciones documentales no cambian esa referencia.

La rama limpia no elimina recursos de la historia pública anterior. El registro histórico de dependencias identifica material Warehouse, Character08 y 3Rifle ya publicado; sus antiguos recuentos de refs no describen el estado remoto actual. La release del juego continúa bloqueada por las condiciones de runtime; su reconciliación con la etiqueta y la publicación de un enlace de descarga siguen pendientes.

No se ejecutaron tests Unity, Play Mode, builds ni el juego. La prueba previa del usuario y 84/84 son antecedentes, no verificaciones de esta entrega documental.
