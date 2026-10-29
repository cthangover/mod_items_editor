# Items Editor — edita los objetos de un mod en un formulario

El Items Editor edita los objetos de un mod mientras el juego está en marcha: nombres y descripciones en todos los idiomas del mod, precio, imagen, tipos de objeto y la acción al usarlos desde el inventario. **Guardar** escribe los archivos del mod y el juego muestra el cambio al instante — sin reiniciar ni recargar.

Se abre desde la caja de herramientas (el martillo de la barra) como **Objetos**.

## Una edición y su resultado en el juego

En RPG Plus el inventario guarda el elixir de la tienda de la aldea:

![El inventario con Elixir seleccionado: Targeted, precio 10.](assets/bag-before.jpg)

En el editor: elige el mod `rpg_demo_game` arriba, selecciona **Elixir** en la lista, escribe un nombre nuevo en inglés y pulsa **Guardar** (Ctrl+S):

![La ventana Objetos sobre rpg_demo_game: la lista Leña, Piedras, Seta del bosque y Dawn elixir; el formulario de rpg_demo/elixir con la clave del nombre rpg_demo/item/elixir/name, texto (en) Dawn elixir y los otros seis idiomas sin cambios, la descripción en siete idiomas, Price 10, Icon rpg_demo/healing_elixir con su imagen, Types TargetUsed, Action action/heal. La línea de estado: «Guardado: 0 archivo(s), 1 texto(s) en el mod rpg_demo_game; el juego releyó sus datos».](assets/editor-saved.jpg)

Solo cambió la línea en inglés del `locale/en.properties` del mod; el archivo del objeto no se tocó. El mismo elixir en el inventario justo después:

![El inventario muestra ahora el objeto como Dawn elixir, sigue Targeted, precio 10.](assets/bag-after.jpg)

## El formulario

- **Id** — el id del objeto; por él lo encuentran escenarios, tiendas, mapas y otros mods.
- **Name** y **Description** — la clave de traducción y una línea por idioma del mod. Un objeto nuevo recibe claves hechas a partir de su id; los textos van a los archivos de localización del mod.
- **Price** — en monedas (las tiendas pueden poner sus propios precios).
- **Icon** — una imagen del grupo de objetos; **…** abre una cuadrícula para elegir una.
- **Types** — Quest, Used, TargetUsed, Food, Resource, Recipe, CantDrop; puede haber varios activados.
- **Action** — qué hace usar el objeto: `null` o una de las acciones de objeto que registran los mods cargados (`action/heal`, `action/equip`…).
- **Campos de otros mods** — los campos que otros mods describen en sus esquemas para los objetos (por ejemplo el tipo de equipo, los rasgos y las clases permitidas de Equipment) aparecen en su propia sección con el mismo tipo de editores.
- **Otros campos** — todo lo demás del JSON del objeto, mostrado y editado como JSON; se conserva tal cual.

La barra: **Nueva** (Ctrl+N) añade `<mod>/new_item` — un recurso que vale una moneda y sin acción; **Copia** (Ctrl+D), **Borrar**, **Deshacer**/**Rehacer** (Ctrl+Z / Ctrl+Y). Guardar escribe solo los archivos cuyo texto cambió; un objeto nuevo va a `items/<mod>.json`.

## Qué objetos se pueden editar

La lista muestra los objetos de un mod — los que están en sus propios archivos. En la lista de mods solo están los instalados como carpetas. Para cambiar el objeto de otro mod sin editarlo, incluye el mismo id en tu mod y declara dependencia del dueño; el propio Items Editor edita solo los archivos del mod seleccionado.

## Si algo sale mal

1. **La lista de mods está vacía.** No hay ningún mod en el que escribir; los mods empaquetados no se pueden editar.
2. **El inventario sigue mostrando el nombre viejo.** El objeto se guardó en otro idioma distinto al del juego; revisa la línea del idioma del juego.
3. **Falta una acción en la lista de acciones.** El mod que la registra no está cargado en este juego.
