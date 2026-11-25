# CommandPalette

A keyboard-driven overlay for navigating pages, data types, workspaces and project commands. Rows can be clicked or driven with the arrow keys and Enter.

## Integration
The palette uses StudioSession.PaletteCommands(), PaletteQuery and PaletteIndex. Each command closes the palette before running its action. It is mounted at the root of the app so it can appear over every workspace.
