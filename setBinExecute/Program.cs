/*
Виноградов С.В. 2023

Программа просматривает каталог и снимает там для определённой группы права на доступ,
но только на те файлы, которые являются исполняемыми
Служит для того, чтобы снять для пользователя возможность исполнения тех программ, которые ему не нужны

main() в файле Program-config.cs
*/


namespace setBinExecute;

using System.Diagnostics;
using System.IO;
using System.Text;

partial class Program
{
    static SortedList<string, string?> viewedDirs = new SortedList<string, string?>(64*1024);
    static SortedList<string, string?> commandsx  = new SortedList<string, string?>(64*1024);
    static SortedList<string, string?> commandsm  = new SortedList<string, string?>(64*1024);
    static SortedList<string, string?> dirToView  = new SortedList<string, string?>(64);


    public readonly static object consoleSync = new Object();
    public readonly static object taskSync    = new Object();
    static void setRights()
    {
        foreach (var conf in configurations)
        {
            foreach (var dir in conf.vals["dirs"])
            {
                if (dirToView.ContainsKey(dir))
                    continue;

                dirToView.Add(dir, null);
            }
        }

        var dirs  = dirToView.Keys;

        // Проходим снизу вверх, чтобы сначала входить в поддиректории
        // Иначе мы помешаем через viewedDirs просматривать поддиректории,
        // которые есть у одного пользователя, но которые уже были пройдены у другого пользователя,
        // у которого эти директории являются поддиректориями просматриваемых директорий
        // (для одного пользователя прошли весь /usr, а потом для другого пытаемся пройти /usr/bin, но viewedDirs уже содержит эту директорию)
        /*for (int i = dirs.Count-1; i >= 0; i--)
            setRights(new DirectoryInfo(dirs[i]), new SortedList<string, Configuration>(0));*/
        Parallel.ForEach
        (
            dirs,
            (di, state, index) =>
            {
                setRights(new DirectoryInfo(di), new SortedList<string, Configuration>(0));
            }
        );

        while (count > 0)
        {
            lock (taskSync)
                Monitor.Wait(taskSync);
        }
    }


    static void setRightsForFilList(List<FileInfo> fiList)
    {
        Parallel.ForEach
        (
            fiList,
            (fi) =>
            {
                var dir   = fi.Directory;
                if (dir == null)
                    return;

                var users = new SortedList<string, Configuration>(16);
                getUsersForDir(dir, users);
Console.Error.WriteLine($"users {users.Count} for file {fi.FullName}");
                ProcessSingleFile(users, fi);
            }
        );
    }

    static volatile int count = 0;
    static          int procc = Environment.ProcessorCount;
    // static DirectoryInfo[] nullDI = new DirectoryInfo[0];
    static void setRights(DirectoryInfo dir, SortedList<string, Configuration> parentUsers)
    {
        dir.Refresh();
        if (!dir.Exists)
            return;

        var users = new SortedList<string, Configuration>(parentUsers);

        DirectoryInfo[] dirs;
        getUsersForDir(dir, users);
        /*
        foreach (var conf in configurations)
            conf.getUsersForDir(dir, users);
*/
        var sb = new StringBuilder(':');
        sb.AppendJoin(':', users.Keys);
        var usersStr = sb.ToString();
        sb.Append(':');
        sb.Clear();

        var drp = getRealpath(dir.FullName);

        lock (viewedDirs)
        {
            if (viewedDirs.ContainsKey(drp))
            {
                if (viewedDirs[drp]!.Contains(usersStr))
                    return;

                // viewedDirs.Remove(drp);
                // Console.Error.WriteLine("!!!!!!!!!!!!!!!!!!!!! viewedDirs.Remove(drp) !!!!!!!!!!!!!!!!!!!!!");
                viewedDirs[drp] = viewedDirs[drp] + usersStr;
            }
            else
                viewedDirs.Add(drp, usersStr);
        }

        dirs = dir.GetDirectories("", SearchOption.TopDirectoryOnly);


        foreach (var dr in dirs)
        {
            Interlocked.Increment(ref count);
            if (count > procc)
            {
                ProcessSubDirectoryWithCounter(users, dr);
            }
            else
                ThreadPool.QueueUserWorkItem
                (
                    delegate
                    {
                        ProcessSubDirectoryWithCounter(users, dr);
                    }
                );
        }

        var files = dir.GetFiles("", SearchOption.TopDirectoryOnly);
        ProcessFiles(users, files);
    }

    private static void getUsersForDir(DirectoryInfo dir, SortedList<string, Configuration> users)
    {
        Parallel.ForEach
        (
            configurations,
            delegate (Configuration conf)
            {
                conf.getUsersForDir(dir, users);
            }
        );
    }

    private static void ProcessFiles(SortedList<string, Configuration> users, FileInfo[] files)
    {
        Parallel.ForEach
        (
            files,
            (fl, state, index) =>
            {
                ProcessSingleFile(users, fl);
            }
        );
    }

    private static void ProcessSingleFile(SortedList<string, Configuration> users, FileInfo fl)
    {
        // Иногда бывают ссылки на файлы, которых нет
        var flr = getRealpath(fl.FullName);
        if (flr == "")
            return;

        if (!File.Exists(flr))
            return;

        var isEx = isExecutable(flr);

        /* if (flr != fl.FullName)
            Console.WriteLine(flr); */

        if (!isEx)
            return;

        var xFacl = new StringBuilder();
        var mFacl = new StringBuilder();
        foreach (var user in users)
        {
            if (user.Value.whites.ContainsKey(flr))
            {
                if (xFacl.Length > 0)
                    xFacl.Append(",");

                xFacl.Append(user.Key);
            }
            else
            {
                if (mFacl.Length > 0)
                    mFacl.Append(",");

                mFacl.Append(user.Key + ":-");
            }
        }

        lock (commandsx)
            if (xFacl.Length > 0)
            {
                var xf = xFacl.ToString();
                xf = $"setfacl -x {xf} \"{flr}\"";
                if (!commandsx.ContainsKey(xf))
                {
                    lock (consoleSync)
                        Console.WriteLine(xf);

                    commandsx.Add(xf, null);
                }
            }

        lock (commandsm)
            if (mFacl.Length > 0)
            {
                var mf = mFacl.ToString();
                mf = $"setfacl -m {mf} \"{flr}\"";
                if (!commandsm.ContainsKey(mf))
                {
                    lock (consoleSync)
                        Console.WriteLine(mf);

                    commandsm.Add(mf, null);
                }
            }
    }

    private static void ProcessSubDirectoryWithCounter(SortedList<string, Configuration> users, DirectoryInfo dr)
    {
        try
        {
            ProcessSubDirectory(users, dr);
        }
        finally
        {
            Interlocked.Decrement(ref count);
            lock (taskSync)
                Monitor.PulseAll(taskSync);
        }
    }

    private static void ProcessSubDirectory(SortedList<string, Configuration> users, DirectoryInfo dr)
    {
        var drr = getRealpath(dr.FullName);

        lock (viewedDirs)
            if (viewedDirs.ContainsKey(drr))
                return;

        var di = new DirectoryInfo(drr);
        setRights(di, users);
    }
}
