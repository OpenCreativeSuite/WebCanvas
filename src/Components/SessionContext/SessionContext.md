# SessionContext

A static context provider that carries the single StudioSession instance through the component tree.

## Integration
Program.cx wraps the app root with Provider(SessionContext.Value, session, root), and every component reads it with UseContext(SessionContext.Value). It is infrastructure rather than a visual component.
