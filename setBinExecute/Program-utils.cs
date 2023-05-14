namespace setBinExecute;

using System.Diagnostics;
using System.IO;

partial class Program
{
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
                Console.Error.WriteLine("chown failed for args: " + args);
            }

            return ec == 0;
        }

        if (exec == "chattr")
        {
            if (ec != 0)
            {
                Console.Error.WriteLine("chattr failed for args: " + args);
            }

            return ec == 0;
        }

        if (exec == "setfacl")
        {
            if (ec != 0)
            {
                Console.Error.WriteLine("setfacl failed for args: " + args);
            }

            return ec == 0;
        }

        return ec == 0;
    }

    // realpath возвращает имена без слешей на конце
    static public string getRealpath(string path)
    {
        try
        {
            var psi = new ProcessStartInfo("realpath", $"\"{path}\"");
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            using var pi  = Process.Start(psi);

            if (pi == null)
                throw new NullReferenceException();

            pi.WaitForExit();

            var rpath = pi.StandardOutput.ReadToEnd(); // Console.WriteLine(rpath);
                rpath = rpath[0 .. ^1];
            return rpath;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("getRealpath: Exception for path: " + path);
            Console.Error.WriteLine(ex.Message);
            Console.Error.WriteLine(ex.StackTrace);
            if (ex.InnerException != null)
            {
                Console.Error.WriteLine(ex.InnerException.Message);
                Console.Error.WriteLine(ex.InnerException.StackTrace);
            }

            throw;
        }
    }

    static public bool isExecutable(DirectoryInfo? dir, string? path = null)
    {
        try
        {
            path ??= dir?.FullName + "/."; //dir?.FullName ?? throw new ArgumentNullException();
            if (dir == null) throw new ArgumentNullException();

            var psi = new ProcessStartInfo("ls", "-al \"" + path + "\"");
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

                var line = pi?.StandardOutput?.ReadLine()?.Trim();
                // Console.WriteLine("line: " + line);
                if (line == null || line.Length <= 0)
                    break;

                // lrwxrwxrwx 1 root root 8 янв 31 18:36 /usr/bin/tclsh -> tclsh8.6
                if (line[0] != '-')
                    return false;

                // -rwx-------...
                if (line[3] == 'x')
                    return true;
                if (line[3] == '-')
                    return false;

                return false;
            }
            while (true);

            throw new Exception("not find need line");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("isExecutable: Exception for path: " + path);
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

}
