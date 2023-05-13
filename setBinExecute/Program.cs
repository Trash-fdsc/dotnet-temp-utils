/*
Программа просматривает каталог и снимает там для определённой группы права на доступ,
но только на те файлы, которые являются исполняемыми
Служит для того, чтобы снять для пользователя возможность исполнения тех программ, которые ему не нужны
*/


namespace setBinExecute;

using System.Diagnostics;
using System.IO;

class Program
{
    static void Main(string[] args)
    {
        // var cd = Directory.GetCurrentDirectory();
        if (args.Length != 3)
        {
            Console.WriteLine("setBinExecute /usr/bin g:noaccess_sbin file.whitelist");
            // Console.WriteLine("setBinExecute /usr/bin g:noaccess_sbin X");
            return;
        }

        var dir   = new DirectoryInfo(args[0]);
        var user  = args[1];
        var files = File.ReadAllLines(args[2].Trim());

        foreach (var fileName in files)
        {
            var fnt = fileName.Trim();
            if (fnt.StartsWith("#") || fnt.Length <= 0)
                continue;

            var fnr = getRealpath(fileName);
            if (whiteFiles.ContainsKey(fnr))
            {
                Console.WriteLine("# Найден повторяющийся файл в списке:\n# " + fileName + "\n# " + fnr + "\n\n");
                continue;
            }

            whiteFiles.Add(fnr, null);
        }


        viewedDirs.Add(dir.FullName, null);
        Console.WriteLine($"# work for directory '{dir.FullName}'");
        Console.WriteLine($"# work with user '{user}'");
        Console.WriteLine($"# work with whitelist '{args[2].Trim()}'");

        setRights(user, dir);

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
                Console.WriteLine("setfacl failed for args: " + args);
            }

            return ec == 0;
        }

        return ec == 0;
    }

    static SortedList<string, string?> viewedDirs = new SortedList<string, string?>(64*1024);
    static SortedList<string, string?> whiteFiles = new SortedList<string, string?>(64*1024);

    static int count = 0;
    static void setRights(string user, DirectoryInfo dir)
    {
         dir.Refresh();
        //Console.SetCursorPosition(0, Console.CursorTop);
        //Console.Write(count);
        count++;

        var dirs  = dir.GetDirectories("", SearchOption.TopDirectoryOnly);
        var files = dir.GetFiles      ("", SearchOption.TopDirectoryOnly);

        foreach (var dr in dirs)
        {   // Console.WriteLine(dr);
            var isi = false;//isImmutable(dr);
            if (isi)
            {
                // Не будем трогать неизменяемые директории
                //ProcessStart("chattr",   $"-i \"{dr.FullName}\"");
            }

            var drr = getRealpath(dr.FullName);
            if (viewedDirs.ContainsKey(drr))
                continue;

            viewedDirs.Add(drr, null);
            setRights(user, new DirectoryInfo(drr));
        }

        foreach (var fl in files)
        {
            var flr  = getRealpath(fl.FullName);
            var isEx = isExecutable(fl.Directory, flr);

            /* if (flr != fl.FullName)
                Console.WriteLine(flr); */

            if (isEx)
            {
                if (whiteFiles.ContainsKey(flr))
                {
                    Console.WriteLine($"sudo setfacl -x {user} \"{flr}\"");
                    continue;
                }

                Console.WriteLine($"sudo setfacl -m {user}:- \"{flr}\"");
            }
        }
    }

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

            return pi.StandardOutput.ReadToEnd().Trim();
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
