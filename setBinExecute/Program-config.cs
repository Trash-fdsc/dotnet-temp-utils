namespace setBinExecute;

using System.Diagnostics;
using System.IO;

partial class Program
{
    // Все сообщения пользователю пишутся в Console.Error
    // Рабочий вывод пишется в стандартный вывод
    static void Main(string[] args)
    {
        // var cd = Directory.GetCurrentDirectory();
        if (args.Length != 1)
        {
            // Console.WriteLine("setBinExecute /usr/bin g:noaccess_sbin file.whitelist");
            Console.Error.WriteLine("setBinExecute config.file");
            return;
        }

        if (!ParseConfig(args))
            return;

        setRights();

        Console.WriteLine();
    }

    public static FileInfo? config_fi = null;

    private static bool ParseConfig(string[] args)
    {
        config_fi = new FileInfo(args[0]); config_fi.Refresh();

        if (!config_fi.Exists)
        {
            Console.Error.WriteLine($"Config file '{config_fi.FullName}' not exists");
            return false;
        }

        var _config_lines = File.ReadAllLines(config_fi.FullName);
        var  config_lines = new List<string>(_config_lines);
        // Сюда вносим новую группу, чтобы при её добавлении как последней групы в предыдущую группу точно скопировались директории из ещё более предыдущей
        // Эта группа реально не добавляется никуда, т.к. после не нет ни одной строки
        config_lines.Add("NEW::");


        Configuration? currentConfiguration = null;
        foreach (var rawLine in config_lines)
        {
            if (currentConfiguration == null)
            {
                currentConfiguration = new Configuration();
                configurations.Add(currentConfiguration);
            }

            var line = rawLine.Trim();
            if (line.Length <= 0 || line.StartsWith("#"))
                continue;

            if (line.StartsWith("NEW::"))
            {
                if (currentConfiguration.vals["dirs"].Count <= 0)
                {
                    if (configurations.Count <= 1)
                    {
                        Console.Error.WriteLine($"Error in the configuration file: the configuration must contain at least one a 'dirs' option in the first group. Error in the configuration group {configurations.Count}");
                        return false;
                    }
                    currentConfiguration.vals["dirs"] = configurations[configurations.Count-2].vals["dirs"];
                }

                if (currentConfiguration.vals["whitelists"].Count <= 0)
                {
                    Console.Error.WriteLine($"Error in the configuration file: whitelists not found in the configuration group {configurations.Count}");
                    return false;
                }

                if (currentConfiguration.vals["users"].Count <= 0)
                {
                    Console.Error.WriteLine($"Error in the configuration file: users/groups not found in the configuration group {configurations.Count}");
                    return false;
                }

                currentConfiguration = null;
                continue;
            }

            if (!currentConfiguration.addLine(rawLine))
                return false;
        }

        var success = true;
        Parallel.ForEach
        (
            configurations,
            delegate (Configuration conf, ParallelLoopState state, long index)
            {
                try
                {
                    if (!conf.ConfigurationProcessing())
                        success = false;
                }
                catch (Exception ex)
                {
                    success = false;
                    Console.Error.WriteLine($"ERROR: {ex.Message}");
                }
            }
        );

        if (!success)
        {
            Console.Error.WriteLine($"An error occurred during whitelist files processing");
            return false;
        }

        return true;
    }

    static List<Configuration> configurations = new List<Configuration>(16);
}
