using System;
using System.IO;
using System.IO.Compression;

internal static class MarkdownDocumentationTests
{
    public static void Run(string directory)
    {
        directory = Path.GetFullPath(directory);
        if (Directory.Exists(directory))
            Directory.Delete(directory, true);
        var html = Path.Combine(directory, "html");
        Directory.CreateDirectory(Path.Combine(html, "_images"));
        File.WriteAllText(Path.Combine(html, "index.html"),
            "<html><body><div role='main'><h1>Sphinx</h1><p><a href='guide.html#usage'>Guide</a></p><img src='_images/diagram.png'></div></body></html>");
        File.WriteAllText(Path.Combine(html, "guide.html"),
            "<html><body><div role='main'><h1 id='usage'>Usage</h1><pre>example()</pre></div></body></html>");
        File.WriteAllText(Path.Combine(html, "_images", "diagram.png"), "image");
        var output = Path.Combine(directory, "markdown");
        MarkdownDocumentation.Build(directory, html, output, requireManifest: false);
        using var zip = ZipFile.OpenRead(output + ".zip");
        if (zip.GetEntry("index.md") == null || zip.GetEntry("_images/diagram.png") == null
            || !File.ReadAllText(Path.Combine(output, "index.md")).Contains("guide.md#usage")
            || !File.ReadAllText(Path.Combine(output, "guide.md")).Contains("example()"))
            throw new Exception("Sphinx conversion, navigation or assets were not preserved.");
        Console.WriteLine("Sphinx Markdown packaging tests passed.");
    }
}
