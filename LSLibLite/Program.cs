using LSLibLite.LS.Save;

namespace LSLibLite;

public static class Program
{
    private static void Main(string[] args)
    {
        if (args.Length is < 2 || args[1] is not { } saveFilePath)
        {
            Console.Error.WriteLine("usage: exe <LSV file>");
            return;
        }

        var helper = new SaveGameHelpers(saveFilePath);
        var resources = helper.LoadGlobals();
        Console.WriteLine($"regions count: {resources.Regions.Count}");
    }
}