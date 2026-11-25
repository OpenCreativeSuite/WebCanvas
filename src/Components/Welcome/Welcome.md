# Welcome

The startup hub shown when no project is open. It offers project creation, opening a folder, recent projects and the Aster & Oak sample.

## Integration
It calls StudioSession.OpenCreateDialog, BrowseForProject, OpenRecentProject and OpenSampleProject. The root app mounts it when HasProject is false.
