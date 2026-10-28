# Instructivo para Diseñadores - CubiCampus

## 1) Editar Estudiantes con Prefab

Los estudiantes ahora se pueden editar con un Prefab sin tocar código.

### Crear el prefab
1. Abre la escena `GameScene`.
2. En Hierarchy, crea o copia un objeto `Estudiante_*` (o crea uno vacío).
3. Asegúrate de que tenga el componente `Student`.
4. Arrástralo a `Assets/Prefabs/` para convertirlo en Prefab.
5. Borra la instancia de la escena.

### Asignarlo en el juego
1. Selecciona el objeto `QueueManager` en `GameScene`.
2. En el componente `QueueManager`, arrastra el prefab `Estudiante` al campo `Estudiante Prefab`.
3. Listo. Ahora todos los alumnos usarán ese prefab.

### Qué se puede editar
- **Visual**: Colores, formas, iconos, barra de estado, etc. (Todo lo que esté en `Visual`)
- **Tamaños/posiciones**: Ajusta los hijos del prefab.
- **Collider**: Modifica `BoxCollider2D` si lo necesitas.

> Si el prefab NO está asignado, el juego seguirá usando el sistema geométrico actual (fallback).

---

## 2) Editar Sonidos sin tocar código

Los sonidos ahora se gestionan con un `AudioConfig` (`ScriptableObject`).

### Crear el AudioConfig
1. Ve a `Assets/Resources/Audio/`
2. Click derecho > `Create > CubiCampus > Audio > Config`
3. Guárdalo como `AudioConfig.asset`

### Asignar sonidos
1. Selecciona `AudioConfig.asset`
2. En `Entradas`, pulsa `+` para añadir un nuevo elemento
3. Configura:
   - `Tipo`: Selecciona el efecto (UiClick, Pickup, Pack, Assign, Happy, Attention, Distraction, Abandon, EarlyLeave, Error, Combo, Bono, Apartado, Rush, Tick, Whistle, Clean, Victory, Defeat, DayStart)
   - `Clip`: Arrastra tu archivo `.wav`/`.mp3`/`.ogg`
   - `Volumen`: Ajusta el volumen relativo
   - `Pitch`: Tono base
   - `Usar Pitch Aleatorio`: Marca para dar variación natural
   - `Variacion Pitch`: Cuánta variación aplicar

### Música (opcional)
También puedes asignar:
- `Musica Menu`
- `Musica Juego`
- `Musica Resultados`

> **Prioridad:** Si hay un `AudioClip` asignado para ese `Sfx` en el `AudioConfig`, se usará ese. Si no hay ninguno, se seguirá usando el sonido procedural generado por código.

---

## Consejos
- Usa `Resources/Audio/AudioConfig.asset` con ese nombre exacto para que se cargue automáticamente.
- Guarda los prefabs en `Assets/Prefabs/`
- Los `ScriptableObject` de audio van en `Assets/Resources/Audio/`
- Cualquier cambio en el prefab o en el AudioConfig se aplica al instante sin recompilar.
