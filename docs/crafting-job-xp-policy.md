# Política de XP de oficios para crafteo

Esta política aplica cuando una receta tiene un oficio (`Jobs`) configurado.

## Regla general

La XP otorgada por crafteo se calcula en servidor con esta prioridad:

1. **Override manual**: si `ExperienceAmount > 0`, se usa ese valor exacto.
2. **Cálculo automático**: si `ExperienceAmount == 0`, se calcula con la fórmula base.

## Fórmula automática

- `baseXp = max(1, cantidadDeIngredientes) * 6`
- `deltaNivel = clamp(nivelReceta - nivelOficioJugador, -10, 10)`
- `multiplicador = 1.0 + (deltaNivel * 0.08)`
- `xpEscalada = round(baseXp * multiplicador)`
- `xpFinal = clamp(xpEscalada, mínimoTramo, máximoTramo)`

Donde el tramo depende de `RecipeLevel`:

- **Low (1-20)**: mínimo `5`, máximo `40`
- **Mid (21-50)**: mínimo `20`, máximo `120`
- **High (51+)**: mínimo `60`, máximo `300`

## Guía para diseñadores de contenido

### Recetas low (1-20)

- Recomendado para onboarding y progresión temprana.
- Mantener entre 1 y 4 ingredientes.
- Dejar `ExperienceAmount = 0` para aprovechar el escalado automático.
- Si una receta tutorial necesita premio fijo, usar override puntual.

### Recetas mid (21-50)

- Recomendado para progresión principal.
- Usar 3 a 6 ingredientes para que la fórmula premie complejidad.
- Ajustar `RecipeLevel` para que la diferencia con el jugador module el ritmo de XP.

### Recetas high (51+)

- Recomendado para endgame y crafteo especializado.
- Usar 5+ ingredientes y requisitos más fuertes.
- Evitar overrides masivos: usar `ExperienceAmount` solo en casos especiales (boss recipes, hitos narrativos, eventos limitados).

## Campos de receta

- `Jobs`: oficio que recibe la XP.
- `RecipeLevel`: nivel de receta usado por la fórmula automática.
- `ExperienceAmount`: override opcional (si es mayor a 0, reemplaza el cálculo automático).

