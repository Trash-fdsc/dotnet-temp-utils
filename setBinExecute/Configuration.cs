namespace setBinExecute;


public class Configuration
{
    public SortedList<string, List<string>> vals   = new SortedList<string, List<string>>(3);
    public SortedList<string, bool>         whites = new SortedList<string, bool>(128);
    public SortedList<string, bool>         tmp    = new SortedList<string, bool>(128);
    public Configuration()
    {
        // Директории для просмотра файлов и изменения в них прав
        vals.Add
        (
            "dirs",
            new List<string>(128)
        );
        // Списки пользователей
        vals.Add
        (
            "users",
            new List<string>(128)
        );
        // Файлы, содержащие белые списки. Списки парсятся в whites
        vals.Add
        (
            "whitelists",
            new List<string>(128)
        );
    }

    public bool addLine(string rawLine)
    {
        
        var splittedLine = rawLine.Split(":", 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries); 
        if (splittedLine.Length != 2)
            return false;

        var name = splittedLine[0];
        var val  = splittedLine[1];

        if (!vals.ContainsKey(name))
        {
            Console.Error.WriteLine($"Configuration file contains unknown field: '{name}'. Known fields:");
            Console.Error.WriteLine("'NEW::'");
            foreach (var key in vals.Keys)
                Console.Error.WriteLine($"'{key}'");

            return false;
        }

        vals[name].Add(val);
        return true;
    }

    public bool ConfigurationProcessing()
    {
        foreach (var whitelistFileName in vals["whitelists"])
        {
            readWhitelist(whitelistFileName);
        }

        return true;
    }

    public bool readWhitelist(string whitelistFileName)
    {
            whitelistFileName = whitelistFileName.Trim();

        if (whitelistFileName.StartsWith("/"))
        {
            Console.Error.WriteLine($"Whitelist file names are specified relative to the location of the configuration file: {whitelistFileName}");
            return false;
        }

        var whitelistFullFileName = Path.Combine( Program.config_fi!.DirectoryName!, whitelistFileName );

        if (!File.Exists(whitelistFullFileName))
        {
            Console.Error.WriteLine($"ERROR: the whitelist file not found: '{whitelistFullFileName}'");
            return false;
        }
        var files = File.ReadAllLines(whitelistFullFileName);

        foreach (var fileName in files)
        {
            var fnt = fileName.Trim();
            if (fnt.StartsWith("#") || fnt.Length <= 0)
                continue;

            var fnr = Program.getRealpath(fileName);
            // Проверяем, что файлы в списке не повторяются, дабы в дальнейшем при удалении файла из белого списка пользователь не забыл одну из копий
            if (tmp.ContainsKey(fileName))
            {
                Console.Error.WriteLine($"Warning: Найден повторяющийся файл в списке {whitelistFileName}:\n{fileName}\n{fnr}\n");
            }
            else
                tmp.Add(fileName, false);

            if (whites.ContainsKey(fnr))
                continue;

            // Если файл есть, то устанавливаем флаг. Если нет, то считаем, что это директория (не проверяем, существует это или нет вообще)
            if (File.Exists(fnr))
                whites.Add(fnr, true);
            else
                whites.Add(fnr, false);
        }

        return false;
    }
}
