namespace setMegaR;

using System.Diagnostics;
using System.IO;

class Program
{
    static void Main(string[] args)
    {
        // var cd = Directory.GetCurrentDirectory();
        if (args.Length != 2)
        {
            Console.WriteLine("setMegaR dirName userName");
            return;
        }

        var dir   = new DirectoryInfo(args[0]);
        var user  = args[1];

        setRights(user, dir, false);

        Console.WriteLine();
    }

    static bool ProcessStart(string exec, string args)
    {
        var pi = Process.Start(exec,   args);

        if (!pi.WaitForExit(1000))
            throw new Exception("!WaitForExit for args: " + args);

        int ec = pi.ExitCode;

        if (exec == "chown")
        {
            if (ec != 0)
            {
                Console.WriteLine("chown failed for args: " + args);
            }

            return ec == 0;
        }

        if (exec == "chattr")
        {
            if (ec != 0)
            {
                Console.WriteLine("chattr failed for args: " + args);
            }

            return ec == 0;
        }

        if (exec == "setfacl")
        {
            if (ec != 0)
            {
                Console.WriteLine("chattr failed for args: " + args);
            }

            return ec == 0;
        }

        return ec == 0;
    }

    static int count = 0;
    static void setRights(string user, DirectoryInfo dir, bool isServiceDirectoryFlag)
    {
        Console.SetCursorPosition(0, Console.CursorTop);
        Console.Write(count);
        count++;

        var dirs  = dir.GetDirectories("", SearchOption.TopDirectoryOnly);
        var files = dir.GetFiles      ("", SearchOption.TopDirectoryOnly);

        foreach (var dr in dirs)
        {   // Console.WriteLine(dr);
            var isi = isImmutable(dr);
            if (isi)
                ProcessStart("chattr",   $"-i \"{dr.FullName}\"");

            ProcessStart("chown",   $":arcs-read --from=:root  \"{dr.FullName}\"");
            ProcessStart("chown",   $":arcs-read --from=:first \"{dr.FullName}\"");

            ProcessStart("chmod",   $"g+rwx \"{dr.FullName}\"");
            ProcessStart("setfacl", $"-m g::rwx \"{dr.FullName}\"");

            var isSD = isServiceDirectoryFlag || isServiceDirectory(dr);
            if (isSD)
                ProcessStart("setfacl", $"-m g:{user}:rwx \"{dr.FullName}\"");
            else
                ProcessStart("setfacl", $"-m g:{user}:rwx \"{dr.FullName}\"");

            setRights(user, dr, isSD);

            if (isi)
                ProcessStart("chattr",   $"+i \"{dr.FullName}\"");
        }

        foreach (var fl in files)
        {
            var isi = isImmutable(fl.Directory, fl.FullName);
            if (isi)
                ProcessStart("chattr",   $"-i \"{fl.FullName}\"");

            ProcessStart("chown",   $":arcs-read --from=:root  \"{fl.FullName}\"");
            ProcessStart("chown",   $":arcs-read --from=:first \"{fl.FullName}\"");

            // ProcessStart("chmod",   $"g+rw-x,a-x \"{fl.FullName}\"");
            // ProcessStart("chmod",   $"u+rw-x,a-x \"{fl.FullName}\"");
            ProcessStart("chmod",   $"g+rw \"{fl.FullName}\"");
            ProcessStart("chmod",   $"u+rw \"{fl.FullName}\"");
            // ProcessStart("setfacl", $"-m g::r   \"{fl.FullName}\"");

            var isSD = isServiceDirectoryFlag || isServiceDirectory(fl.Directory);
            if (isSD)
                ProcessStart("setfacl", $"-m g:{user}:rw \"{fl.FullName}\"");
            else
                ProcessStart("setfacl", $"-m g:{user}:r \"{fl.FullName}\"");

            if (isi)
                ProcessStart("chattr",   $"+i \"{fl.FullName}\"");
        }
    }

    static public bool isImmutable(DirectoryInfo? dir, string? path = null)
    {
        try
        {
            path ??= dir?.FullName + "/."; //dir?.FullName ?? throw new ArgumentNullException();
            if (dir == null) throw new ArgumentNullException();

            var psi = new ProcessStartInfo("lsattr", "-a \"" + dir.FullName + "\"");
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            var pi  = Process.Start(psi);

            if (pi == null)
                throw new NullReferenceException();
/*
            if (!pi.WaitForExit(1000))
                throw new Exception("!WaitForExit for dir " + dir.FullName);
*/
            // Console.WriteLine("path: " + path);
            var cnt = 0;
            do
            {
                cnt++;
                /* if (cnt > 1024)
                    Console.WriteLine("AAAAAAAAAAAAAAA: " + dir.FullName); */

                var line = pi.StandardOutput.ReadLine();
                // Console.WriteLine("line: " + line);
                if (line == null)
                    break;

                if (line.EndsWith("/.."))
                    continue;

                if (!line.EndsWith(" " + path))
                    continue;

                // ----i---------e-------
                if (line[4] == 'i')
                    return true;

                return false;
            }
            while (true);

            throw new Exception();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("isImmutable: Exception for path: " + path);
            Console.Error.WriteLine(ex.Message);
            Console.Error.WriteLine(ex.StackTrace);
            if (ex.InnerException != null)
            {
                Console.Error.WriteLine(ex.InnerException.Message);
                Console.Error.WriteLine(ex.InnerException.StackTrace);
            }

            return false;
        }
    }

    static public bool isServiceDirectory(DirectoryInfo? dir)
    {
        if (dir == null)
            return false;

        if (dir.Name == ".debris")
		{
            return true;
		}

        if (dir.Name == ".sync")
            return true;

        return isServiceDirectory(dir.Parent);
    }
}
