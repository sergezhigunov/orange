using System.Reflection;

namespace Orange.Ide.App.Resources;

internal static class ResourceLoader
{
    private const string SampleFilesPrefix = "Orange.Ide.App.Resources.SampleFiles.";

    internal static string LoadSampleFile(string fileName)
    {
        Stream? stream = typeof(ResourceLoader).GetTypeInfo().Assembly.GetManifestResourceStream(SampleFilesPrefix + fileName);

        if (stream == null)
            return string.Empty;

        using (stream)
        using (StreamReader reader = new(stream))
        {
            return reader.ReadToEnd();
        }
    }
}