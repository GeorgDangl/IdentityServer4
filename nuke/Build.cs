using System;
using System.Linq;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.WebDocu.WebDocuTasks;
using Nuke.Common.Tools.Docker;
using Nuke.WebDocu;

class Build : NukeBuild
{
    public static int Main() => Execute<Build>(x => x.BuildDocumentation);

    AbsolutePath DocsDirectory => RootDirectory / "docs";
    AbsolutePath DocsOutputDirectory => DocsDirectory / "_build" / "html";

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Parameter] readonly string DocuBaseUrl;
    [Parameter] readonly string DocuApiKey;
    [Parameter] readonly string DocsVersion;

    Target BuildDocumentation => _ => _
        .Executes(() =>
        {
            EnsureCleanDirectory(DocsOutputDirectory);
            DockerTasks.Docker($"run -v {DocsDirectory}:/docs --rm sphinxdoc/sphinx ./generate_docs.sh");
        });

    Target UploadDocumentation => _ => _
        .DependsOn(BuildDocumentation)
        .Requires(() => DocuApiKey)
        .Requires(() => DocuBaseUrl)
        .Requires(() => DocsVersion)
        .Executes(() =>
        {
            WebDocu(s => s
                .SetDocuBaseUrl(DocuBaseUrl)
                .SetDocuApiKey(DocuApiKey)
                .SetSourceDirectory(DocsOutputDirectory)
                .SetVersion(DocsVersion)
                .SetSkipForVersionConflicts(true));
        });
}
