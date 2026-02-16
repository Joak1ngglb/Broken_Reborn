# Migración: Boss por flag `IsBoss` (sin prefijo en nombre)

## Cambio aplicado
A partir de este cambio, la política de boss queda unificada en un único origen de verdad:

- El multiplicador de XP de boss depende **solo** de `NPCDescriptor.IsBoss`.
- El estilo visual del nombre de boss en cliente depende **solo** del flag `IsBoss` serializado por el servidor.

El prefijo legacy `[BOSS]` en `Name` ya no se usa para lógica de gameplay ni para estilizado visual.

## Acciones recomendadas de migración
1. Revisar NPCs existentes y asegurarse de marcar `IsBoss = true` en los que correspondan.
2. Remover el prefijo `[BOSS]` del campo `Name` en todos los NPCs donde aún exista.
3. Validar en juego:
   - XP otorgada por boss.
   - Estilo visual de nombre de boss.

## Checklist rápida
- [ ] Todos los bosses tienen `IsBoss = true`.
- [ ] No quedan nombres con prefijo `[BOSS]`.
- [ ] El multiplicador de XP de boss se aplica correctamente.
- [ ] El cliente renderiza estilo de boss usando `packet.IsBoss`.
