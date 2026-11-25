# DevBuild

The build console: pretty-URL and sitemap options, output folder, build result summary, warnings and the build log.

## Integration
It calls StudioSession.RunBuild, reads BuildResult, BuildLog and writes options through SetBuildOption. The Development workspace selects it with DevSection.Build.
