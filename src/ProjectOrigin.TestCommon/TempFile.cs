using System.IO;

namespace ProjectOrigin.TestCommon;

public static class TempFile
{
    public static string WriteAllText(string content)
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, content);
        return path;
    }
}
