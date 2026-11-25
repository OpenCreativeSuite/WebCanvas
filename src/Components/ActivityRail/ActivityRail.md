# ActivityRail

The narrow vertical rail that switches between Content, Design, Blocks, Data, Automation and Development. It also exposes the command palette, theme toggle and lower utility actions.

## Integration
ActivityRail reads StudioSession.ActiveMode to mark the current mode and calls OpenMode, OpenPalette and ToggleTheme. It is mounted inside the application shell next to the contextual sidebar and workspace host.
