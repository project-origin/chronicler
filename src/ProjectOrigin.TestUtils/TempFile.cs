using System.IO;

namespace ProjectOrigin.TestUtils;

public static class TempFile
{
    public static string WriteAllText(string content)
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, content);
        return path;
    }
}
