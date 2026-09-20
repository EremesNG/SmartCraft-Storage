# Referentes para el terminal administrador de SmartCraftStorage

Investigación del 19 de septiembre de 2026. Exploración previa a una especificación: las recomendaciones de este informe todavía no son decisiones de implementación. Se inspeccionaron fuentes y código; no se ejecutaron estos mods ni sus pruebas en Valheim.

La conclusión es combinar la experiencia de inventario unificado de OdinStorage, la separación entre planificación y ejecución de ChestButler y las comprobaciones de identidad de objetos de Quartermaster. El criterio de distribución de SCS debe responder al inventario común que pidió el usuario: aprovechar capacidad y liberar cofres, permitiendo que distintos materiales compartan espacio.

## Alcance ya acordado

El terminal administra un inventario lógico finito respaldado por cofres físicos. El jugador busca, deposita y retira desde su ventana, sin necesitar conocer la ubicación física del objeto. Los cofres guardan los objetos; el terminal presenta y administra ese conjunto.

La propuesta de vinculación inicial es nombre de red compartido, radio configurable y permisos de wards. Tras revisar los referentes, el usuario pidió analizar una ampliación: que las mesas y procesadores detecten el terminal tanto como fuente de recursos como destino de su producción automática. Esto sustituye la exclusión anterior para esos usos en el diseño que se está evaluando. Shift+E conserva su alcance directo previo; la construcción con martillo no se incluye automáticamente por compartir código con el crafteo. Se permite requerir SCS en servidor y clientes y apoyarse en MultiUserChest.

## Fuentes y versiones inspeccionadas

| Referente | Evidencia y alcance |
| --- | --- |
| Quartermaster | Código de `master`, commit [`9550541`](https://github.com/Vassteel/Quartermaster/tree/9550541feddcc570869aab4e95bf825b17cbba72), manifiesto 0.1.16. También se comparó el núcleo con la rama de desarrollo [`233820d`](https://github.com/Vassteel/Quartermaster/tree/233820d6403d00b76b5a8055c0d112982db58274), cuyo manifiesto dice 0.1.27. Son referencias identificadas; no se presupone equivalencia entre ramas y paquetes publicados. |
| ChestButler | Código del tag publicado [`v2.1.2`](https://github.com/EladKarni/ChestButler/releases/tag/v2.1.2), commit `40cda3f12f0c3348c501b963ab6fe7c8a6f3ea73`, contrastado con la [ficha del autor](https://thunderstore.io/c/valheim/p/EK_Solutions/ChestButler/). |
| OdinStorage | Inspección anterior del README y del IL de la copia local 0.9.2 facilitada por el usuario. Sigue siendo referencia de interfaz; no es una verificación de funcionamiento multijugador. |
| SmartCraftStorage | Revisión local en `storage-net`, commit `805e79c`. Se comprobaron las búsquedas físicas y las escrituras existentes en `NearbyContainers`, quick stack, crafting y smelters. |

Son fuentes primarias de sus autores. El código permite verificar mecanismos; los anuncios de seguridad y los resultados de pruebas declarados por los autores no equivalen a pruebas realizadas en este entorno. La búsqueda Exa de Quartermaster devolvió cinco resultados, incluidos paquetes de distintas versiones, que se contrastaron con el código. La investigación de los dos proyectos se dividió en dos frentes de lectura.

## Quartermaster: aportes y diferencias

Mantiene un registro de cofres cargados, comprueba acceso y wards y filtra por radio y grupo normalizado. La pertenencia al grupo y los tipos de objeto recordados son conceptos separados. Esto es útil para que SCS distinga «pertenece a esta red» de «contiene este material». [ContainerRegistry](https://github.com/Vassteel/Quartermaster/blob/9550541feddcc570869aab4e95bf825b17cbba72/src/ContainerRegistry.cs), [Policy](https://github.com/Vassteel/Quartermaster/blob/9550541feddcc570869aab4e95bf825b17cbba72/src/Policy.cs).

Su comprobación de apilado compara prefab/tipo, calidad, variante, nivel de mundo, datos del creador, durabilidad y datos personalizados. Además calcula capacidad parcial y respeta el máximo de cada stack. Es una referencia útil para no confundir objetos que comparten nombre pero tienen propiedades diferentes. [InventoryTransfers, líneas 39–109](https://github.com/Vassteel/Quartermaster/blob/9550541feddcc570869aab4e95bf825b17cbba72/src/InventoryTransfers.cs#L39-L109).

Los depósitos se distribuyen según tipos aprendidos, destinos preferidos y destinos de desbordamiento. La ordenación automática fusiona y ordena dentro de cada cofre. La guía declara que no redistribuye globalmente los cofres existentes. Ese objetivo difiere de la compactación de toda la red solicitada para SCS. [Automation, líneas 111–172](https://github.com/Vassteel/Quartermaster/blob/9550541feddcc570869aab4e95bf825b17cbba72/src/Automation.cs#L111-L172), [Guide, líneas 31–39](https://github.com/Vassteel/Quartermaster/blob/9550541feddcc570869aab4e95bf825b17cbba72/GUIDE.md#L31-L39).

Su ruta de escritura exige que el cliente ya sea dueño del cofre. La guía reconoce que la propiedad repartida puede detener movimientos y que falta validar multijugador. La rama de desarrollo comparada mantiene esa condición. Por ello, esta arquitectura de transferencia no resuelve por sí sola el caso del usuario como cliente remoto. [ContainerRegistry, líneas 45–56](https://github.com/Vassteel/Quartermaster/blob/9550541feddcc570869aab4e95bf825b17cbba72/src/ContainerRegistry.cs#L45-L56), [Guide, líneas 69–75](https://github.com/Vassteel/Quartermaster/blob/9550541feddcc570869aab4e95bf825b17cbba72/GUIDE.md#L69-L75).

## ChestButler: aportes y diferencias

Separa un planificador determinista, independiente de Unity, de la ejecución en el mundo. El planificador administra un presupuesto común de slots para evitar prometer el mismo espacio a varios grupos, conserva hogares elegidos en ejecuciones anteriores y admite compartir capacidad entre grupos. Su prioridad incluye hogares y categorías; no implementa exclusivamente el objetivo de utilizar la menor cantidad posible de cofres. [OrganizePlanner](https://github.com/EladKarni/ChestButler/blob/40cda3f12f0c3348c501b963ab6fe7c8a6f3ea73/src/ChestButler/Core/OrganizePlanner.cs#L696-L788).

El Puller muestra materiales disponibles y los trae a su propio inventario físico. Su función de retirada suma la cantidad solicitada inmediatamente. Para SCS, la experiencia propuesta es retirar desde la vista común al jugador, sin convertir el terminal en otro almacén permanente. [PullerStorage](https://github.com/EladKarni/ChestButler/blob/40cda3f12f0c3348c501b963ab6fe7c8a6f3ea73/src/ChestButler/Core/PullerStorage.cs#L73-L139).

El registro de contenedores filtra distancia y acceso, con orden estable por distancia e identificador. En el código inspeccionado la pertenencia es espacial; no implementa nuestra vinculación por nombre de red. [ContainerTracker](https://github.com/EladKarni/ChestButler/blob/40cda3f12f0c3348c501b963ab6fe7c8a6f3ea73/src/ChestButler/Core/ContainerTracker.cs#L44-L104).

En `Organizer` hay tres detalles que condicionan su adaptación:

- La identidad usada para presupuestar apilado es nombre normalizado y nivel de mundo. No incluye calidad ni datos personalizados.
- Antes de emitir movimientos revalida origen, destino, acceso y capacidad. Puede retirar desde un origen remoto mediante MUC, pero pospone un destino que conserva otro dueño.
- Observa éxito y cantidad de las respuestas. Sin embargo, si una solicitud desaparece sin respuesta registrada, suma la cantidad solicitada como movida y también la marca no verificada. Al vencer un timeout libera su reserva; eso no demuestra que la operación remota haya sido cancelada.

Estas observaciones provienen de [Organizer, identidad](https://github.com/EladKarni/ChestButler/blob/40cda3f12f0c3348c501b963ab6fe7c8a6f3ea73/src/ChestButler/Core/Organizer.cs#L129-L179), [emisión](https://github.com/EladKarni/ChestButler/blob/40cda3f12f0c3348c501b963ab6fe7c8a6f3ea73/src/ChestButler/Core/Organizer.cs#L723-L817) y [contabilización](https://github.com/EladKarni/ChestButler/blob/40cda3f12f0c3348c501b963ab6fe7c8a6f3ea73/src/ChestButler/Core/Organizer.cs#L888-L937). La observación de respuestas usa un parche Harmony sobre MUC: [MucResponsePatches](https://github.com/EladKarni/ChestButler/blob/40cda3f12f0c3348c501b963ab6fe7c8a6f3ea73/src/ChestButler/Patches/MucResponsePatches.cs).

Conviene adoptar la separación entre plan y ejecución, las reservas de capacidad y la revalidación. Su código no prueba una transacción global entre todos los cofres ni una garantía absoluta ante desconexiones. Esa conclusión es un límite de la evidencia estática, no un fallo de pérdida de objetos reproducido.

## Propuesta que encaja con el manager solicitado

1. **Una vista común.** Buscar, filtrar y ordenar por nombre o cantidad cambia la presentación. La vista mantiene referencias a los objetos reales y sus cantidades disponibles; no guarda una segunda copia de los objetos. Las variantes incompatibles se distinguen al retirar.
2. **Depósitos que aprovechan capacidad.** Primero completar stacks compatibles de la red; después ocupar huecos de cofres ya utilizados; abrir espacio en otro cofre cuando haga falta. Si solo cabe una parte, mostrar cuánto se confirmó y conservar o recuperar explícitamente el resto mediante el protocolo de transferencia.
3. **Organizar físicamente.** Fusionar stacks compatibles y consolidar en menos cofres, permitiendo mezclas. Entre soluciones igualmente compactas, preferir la que mueva menos objetos. Un segundo Organizar sin cambios externos debería producir cero movimientos. No crear reservas permanentes por tipo de material como política inicial.
4. **Ejecución coordinada.** Separar movimientos planeados, pendientes, confirmados y de resultado desconocido. Reservar capacidad para solicitudes pendientes, validar de nuevo antes de mover y conciliar estados inciertos antes de reintentar. MUC puede ser el transporte; la coordinación de una operación de red sigue siendo responsabilidad de SCS.

Una previsualización útil sería «se combinarán 7 stacks y quedará libre 1 cofre». El resultado debe distinguir slots liberados por fusión y cofres vaciados por redistribución: trasladar stacks enteros no crea slots adicionales.

Ejemplo con cuatro cofres de diez slots y stacks completos:

| Estado | Cofre 1 | Cofre 2 | Cofre 3 | Cofre 4 |
| --- | --- | --- | --- | --- |
| Antes | 10 A | 5 A | 10 B | 5 B |
| Después | 10 A | 5 A + 5 B | 10 B | Vacío |

Se utilizan tres cofres, con los mismos treinta slots ocupados sobre una capacidad total de cuarenta: siguen quedando diez libres. Si también había stacks parciales compatibles, fusionarlos puede reducir los slots ocupados.

## Interacción con las funciones actuales de SCS

En el código actual la ubicación importa para crafting y procesadores: buscan cofres alrededor del jugador o de la máquina. Sin integración del terminal, mover carbón de un cofre próximo al horno a otro lejano puede dejarlo fuera de alcance. Véanse [búsqueda del smelter](../../Stations/SmelterPatches.cs#L225) y [búsqueda de crafting](../../CraftingChestAccess/InventoryChestPatches.cs#L42).

La ampliación solicitada permite resolver esa limitación: un terminal cercano proporciona acceso a sus miembros válidos, aunque estos estén fuera del radio directo de la máquina. Organizar puede cambiar la distribución interna conservando ese acceso mientras los cofres sigan vinculados, cargados y autorizados. El terminal introduce un alcance indirecto deliberado, con dos comprobaciones independientes: consumidor a terminal y terminal a cofre.

También hay una interacción de concurrencia: SCS ya escribe directamente en inventarios después de solicitar propiedad del cofre. Su propio helper indica que reclamar propiedad no es un bloqueo. Mantener el comportamiento visible de Shift+E y procesadores exige comprobar cómo esas escrituras conviven con los movimientos pendientes del manager. [NearbyContainers, líneas 153–198](../../Shared/NearbyContainers.cs#L153).

## Ampliación analizada: terminal como fuente y destino de estaciones

La experiencia propuesta es que una mesa vea los materiales de la red, un horno obtenga mineral y combustible a través del terminal, y la producción que SCS ya recoge automáticamente se distribuya de vuelta a los cofres vinculados. El manager decide qué cofre entrega o recibe. El resultado del crafteo manual continúa en el inventario del jugador; la ampliación de destinos se refiere a las salidas automáticas de los procesadores.

Ejemplo: un horno detecta el terminal a 8 metros y este vincula cofres a 50 metros. Si ambos enlaces cumplen sus radios y permisos, el horno puede consumir carbón de un miembro y almacenar el lingote en otro. El ejemplo describe la propuesta, no una capacidad ya implementada.

El cambio tiene alcance transversal en SCS:

1. **Unificar acceso al almacenamiento.** Cofres normales y terminales necesitan una interfaz común para consultar recursos, retirar, calcular capacidad y depositar. La red mantiene referencias a los inventarios reales y debe poder descubrirse y actualizarse con la ventana del terminal cerrada. Una copia agregada del inventario Vanilla no debe convertirse en otra fuente de verdad.
2. **Resolver cada cofre una sola vez.** Si un cofre está en rango directo y también detrás de un terminal, sus recursos y huecos cuentan una vez. Lo mismo ocurre con redes solapadas. La identidad estable del cofre físico, como su ZDO, debe servir para deduplicar. Los terminales no se vinculan recursivamente entre sí en esta propuesta.
3. **Coordinar consumo y alimentación.** El crafteo requiere confirmar el coste completo antes de conceder el resultado. Un procesador necesita confirmar la retirada y reservar su propia capacidad de entrada antes de recibir combustible o material, sin que cada actualización solicite otra vez los mismos insumos. Si la máquina desaparece o rechaza la entrada, hay que recuperar lo retirado.
4. **Coordinar producción y depósitos.** Reservar espacio compatible en la red, conservar el producto en un estado pendiente recuperable mientras se confirma y registrar la cantidad realmente entregada. Una falta de respuesta no equivale a falta de espacio. El sobrante confirmado puede seguir la política de salida actual; una cantidad de resultado desconocido necesita conciliación antes de reintentarse o soltarse al suelo.
5. **Compartir coordinación con las operaciones existentes.** Organizar, retirar desde la ventana, crafting y automatización pueden afectar el mismo stack o hueco. Los escritores directos existentes también deben respetar las reservas, aunque no obtengan acceso a la red a través del terminal. Los conteos, límites de producción e invalidaciones de caché deben usar el mismo conjunto de recursos y operaciones pendientes.

Estas necesidades se apoyan en los flujos actuales: [InventoryChestPatches](../../CraftingChestAccess/InventoryChestPatches.cs) consulta y consume mediante llamadas inmediatas; [SmelterPatches](../../Stations/SmelterPatches.cs#L73) retira ingredientes y añade a la máquina, y su salida calcula inmediatamente cuánto logró depositar; [FermenterPatches](../../Stations/FermenterPatches.cs#L88) deposita lo que cabe y genera el sobrante en el mundo.

MUC ofrece solicitudes por cofre y una respuesta inmediata cuando el cliente ya es el dueño, o una solicitud RPC para el caso remoto. Su API inspeccionada no ofrece una operación de receta completa sobre varios cofres. SCS necesita resolver esa coordinación y la recuperación de resultados inciertos. [ContainerHandler](https://raw.githubusercontent.com/MSchmoecker/No-Chest-Block/master/MultiUserChest/ContainerHandler.cs), [ContainerRPCHandler](https://raw.githubusercontent.com/MSchmoecker/No-Chest-Block/master/MultiUserChest/ContainerRPCHandler.cs).

La implementación debería conservar las comprobaciones de acceso al terminal y a cada miembro para la identidad que autoriza la operación. El propietario de red de un objeto no sustituye los permisos de un jugador. Actualmente varios procesadores de SCS dependen de `Player.m_localPlayer`; la propuesta no presupone automatización de zonas descargadas ni funcionamiento sin jugadores. Cambiar ese modelo requeriría concretar quién autoriza el trabajo automático.

La transparencia propuesta abarca al jugador y las funciones de SCS integradas. Otros mods que leen directamente `Container.GetInventory()` necesitarían un contrato o adaptación adicional; reconocer visualmente una pieza como cofre no les proporciona automáticamente acceso a toda la red.

Evaluación: es una ampliación de complejidad alta respecto al terminal limitado a su ventana. Es coherente con el objetivo de almacenamiento administrado, pero requiere diseñar desde el inicio la capa común y los estados de transferencia. Se puede verificar progresivamente —crafteo, alimentación y salidas— manteniendo los tres como alcance objetivo. Esta revisión no implementa ni valida en juego esos contratos. La inspección adicional del ensamblado Vanilla no se realizó porque no estaba en la ruta predeterminada; el análisis del flujo actual se basa en los parches de SCS y las fuentes de MUC.

## Evidencia pendiente antes de implementar

La política de capacidad común está suficientemente clara para orientar el diseño. Antes de cerrar una especificación habrá que concretar la coordinación entre clientes, la identidad completa de los stacks y cómo resolver solicitudes cuyo resultado sea desconocido. También se debe fijar la versión de MUC y comprobar sus contratos reales.

La verificación de una futura implementación deberá cubrir dos jugadores retirando el mismo stack, depósito parcial, inventario del jugador lleno, cambio de dueño, desconexión con una operación pendiente, cambios de wards, destrucción o descarga de un cofre, objetos con datos personalizados y escrituras simultáneas de las funciones actuales de SCS. Son pruebas propuestas; esta investigación no las ejecutó.
