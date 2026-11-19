using Serilog;
using System;
using System.Linq;
using Fallout.Common;
using Fallout.Common.CI;
using Fallout.Common.Execution;
using Fallout.Common.IO;
using Fallout.Common.ProjectModel;
using Fallout.Common.Tooling;
using Fallout.Common.Utilities.Collections;
using static Fallout.Common.EnvironmentInfo;
using static Fallout.Common.IO.PathConstruction;
using static Fallout.WebDocu.WebDocuTasks;
using Fallout.Common.Tools.Docker;
using Fallout.WebDocu;

class Build : FalloutBuild
{
    Target TestMarkdownDocumentation => _ => _
        .Executes(() => MarkdownDocumentationTests.Run(RootDirectory / "output" / "markdown-tests"));

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
            DocsOutputDirectory.CreateOrCleanDirectory();
            DockerTasks.Docker($"run -v {DocsDirectory}:/docs --rm sphinxdoc/sphinx ./generate_docs.sh");

            MarkdownDocumentation.Build(RootDirectory, DocsOutputDirectory,
                RootDirectory / "output" / "docs-markdown", requireApi: false, requireManifest: false);
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
                    .SetMarkdownPackagePath(RootDirectory / "output" / "docs-markdown.zip")
                    .SetVersion(DocsVersion)
                    .SetSkipForVersionConflicts(true));
        });
}
