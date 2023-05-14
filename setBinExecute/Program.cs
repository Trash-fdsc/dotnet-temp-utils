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

partial class Program
{
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
}
