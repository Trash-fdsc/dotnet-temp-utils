namespace setBinExecute;

using System.Diagnostics;
using System.IO;

partial class Program
{
    static List<FileInfo>? ParseAideLog(string reportFileName)
    {
        var reportFile_fi  = new FileInfo(reportFileName);
        reportFile_fi.Refresh();
        if (!reportFile_fi.Exists)
            return null;

        // Парсинг взят из smalls/aide-clamav/
        var reportLines = File.ReadAllLines(reportFile_fi.FullName);
        var fs          = new List<FileInfo>(reportLines.Length);
        Parallel.ForEach
        (
            reportLines,
            delegate (string line, ParallelLoopState _state, long _)
            {
                // "d" - это директория - их мы не проверяем, только файлы
                // "f" - строки всех файлов начинаются на букву "f"
                if (!line.Contains(":") || !line.StartsWith("f"))
                    return;

                var sLine = line.Split(':', 2, StringSplitOptions.RemoveEmptyEntries);
                if (sLine.Length < 2)
                    return;

                var fileName =  sLine[1].TrimStart();
                if (fileName.Length <= 0)
                    return;

                // Получаем имя файла для антивирусной проверки
                var fi = new FileInfo(fileName);
                fi.Refresh();
                if (!fi.Exists) // fi.LinkTarget != null
                {
                    // Console.WriteLine("File skipped: " + fi.FullName);
                    return;
                }

                lock (fs)
                {
                    fs.Add(fi);
                }
            }
        );

        // fs.Sort();

        return fs;
    }
}
