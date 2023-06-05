namespace ls;

using System.Diagnostics;


class Program
{
    static int Main(string[] args)
    {
        Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Idle;

        var dir = System.AppContext.BaseDirectory!;
        if (args.Length > 0)
            dir = args[0];

        var di = new DirectoryInfo(dir);
        di.Refresh();
        if (!di.Exists)
        {
            Console.Error.WriteLine("Directory does not exists");
            return 1;
        }

        var dirs = di.GetDirectories();
        foreach (var sdi in dirs)
        {
            Console.WriteLine(sdi.FullName);
        }

        var files = di.GetFiles();
        foreach (var fi in files)
        {
            Console.WriteLine(fi.FullName);
        }

        return 0;
    }
}
