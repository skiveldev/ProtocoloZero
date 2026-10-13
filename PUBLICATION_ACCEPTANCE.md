# Aceptación personal antes de publicar el compilado

**PENDIENTE DE ACEPTACIÓN.** El ZIP con avisos oficiales y su checksum ya están cargados en la release **DRAFT** `v1.0.0-pa4`, con digests confirmados por GitHub. No autoriza publicación pública ni entrega privada del compilado. Nadie acepta ni certifica obligaciones en nombre de `skiveldev`.

## Acción del responsable `skiveldev`

1. Leer personalmente los acuerdos binarios oficiales de [DirectStorage 1.3.0](https://www.nuget.org/packages/Microsoft.Direct3D.DirectStorage/1.3.0/License) y [D3D12 1.618.1](https://www.nuget.org/packages/Microsoft.Direct3D.D3D12/1.618.1/License). Si tiene autoridad y realmente los acepta, documentar la aceptación, incluidas las obligaciones de indemnizar, defender y mantener indemne a Microsoft. La funcionalidad primaria significativa del juego propio ya está documentada; no se requiere ejecutar nuevas pruebas.
2. Confirmar, con evidencia apropiada, las licencias particulares de los recursos y del equipo y la autoridad para distribuir el código propio. Consultar los [términos Asset Store](https://unity.com/legal/as-terms), las [preguntas oficiales Asset Store](https://assetstore.unity.com/browse/eula-faq) y las [preguntas oficiales Mixamo](https://helpx.adobe.com/creative-cloud/faq/mixamo-faq.html). Los permisos generales de uso integrado no prueban las adquisiciones ni la cobertura de cada integrante. No se pide elegir una licencia de código abierto ni se concede una por defecto.
3. Documentar el mecanismo real que **requiera** a destinatarios externos y distribuidores aceptar términos suficientemente protectores para Microsoft y el software conforme a ambos acuerdos (DirectStorage 2(b) y requisitos de distribución de D3D12). Identificar los términos, a quiénes se aplican y cómo se exige y acredita su aceptación. Un README o una declaración «acepto» del propietario no demuestra el acuerdo de terceros; no se presupone un formato universal obligatorio de clic.
4. Documentar la aplicabilidad y cobertura de las condiciones del [corpus oficial Unity Windows Mono Player 6000.5.10f1](https://storage.googleapis.com/live-platform-resources-prd/templates/assets/Player_Windows_Mono_6000_5_10f1_a62c405a91/Player_Windows_Mono_6000_5_10f1_a62c405a91.pdf), también incluido en `Legal/Unity/`: CPOL 1.02, sección 5e (acuerdo de destinatarios) y sección 7 (indemnidad), notificación triangle-strip y TetGen bajo acuerdo negociado de 2012. Los acuerdos previos de Unity pueden cubrir el runtime: no se afirma que haga falta una nueva licencia TetGen independiente. Una afirmación del propietario sin evidencia no prueba esa cobertura; obtener aclaración de Unity si resulta necesaria.

## Plantilla de confirmación — completar solo con hechos ciertos

```text
Responsable y fecha: [identidad pública autorizada / fecha]
Autoridad para asumir y aceptar los acuerdos: [alcance y evidencia]
DirectStorage 1.3.0 y D3D12 1.618.1: [lectura y aceptación personal reales,
incluida indemnidad; fecha y registro, o condición aún pendiente]
Recursos/equipo y código propio: [licencias aplicables, cobertura y evidencia]
Destinatarios/distribuidores: [términos protectores concretos y versión;
mecanismo obligatorio, alcance y evidencia de aceptación/cumplimiento]
Unity/CPOL/triangle-strip/TetGen: [aplicabilidad, cobertura y evidencia;
condiciones satisfechas o pendientes, incluida aclaración del proveedor]
Condiciones aún pendientes: [detalle; «ninguna» solo si está acreditado]
Publicación final solicitada: [solo si todas las condiciones están cerradas]
```

Conservar la evidencia privada por un canal apropiado; no publicar contratos personales, correos ni datos sensibles. El operador principal debe registrar los hechos reales y comprobar que no quedan condiciones aplicables abiertas antes de la publicación final condicional. La urgencia, esta plantilla, los avisos o un README no constituyen aceptación pasada. Una respuesta incompleta mantiene el estado **PENDIENTE DE ACEPTACIÓN**. Véase [estado de entrega](DELIVERY_STATUS.md).
