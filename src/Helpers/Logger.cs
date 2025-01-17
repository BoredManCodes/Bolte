using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using Colorful;
using LogSeverity =  Discord.LogSeverity;
using Discord.WebSocket;
using Gommon;
using Sentry;
using Volte.Entities;
using Volte.Services;
using Console = Colorful.Console;

namespace Volte.Helpers
{
    public static class Logger
    {
        public static bool CaptureExceptionInSentry { get; set; } = true;

        static Logger() => Directory.CreateDirectory("logs");

        private static readonly object sysoutLock = new object();
        private const string _logFilePath = "logs/Volte.log";
        private static bool _headerPrinted;

        public static Task HandleLogAsync(LogEventArgs args)
            => Task.Run(() => Log(
                args.LogMessage.Severity, args.LogMessage.Source, args.LogMessage.Message, args.LogMessage.Exception
            ));

        internal static void PrintHeader()
        {
            if (_headerPrinted) return;
            Info(LogSource.Volte, CommandsService.Separator.Trim());
            Info(LogSource.Volte, $"BBBBBBBBBBBBBBBBB                    lllllll         tttt                                   TM");
            Info(LogSource.Volte, $"B::::::::::::::::B                   l:::::l      ttt:::t                                     ");
            Info(LogSource.Volte, $"B::::::BBBBBB:::::B                  l:::::l      t:::::t                                     ");
            Info(LogSource.Volte, $"BB:::::B     B:::::B                 l:::::l      t:::::t                                     ");
            Info(LogSource.Volte, $"  B::::B     B:::::B   ooooooooooo    l::::lttttttt:::::ttttttt        eeeeeeeeeeee           ");
            Info(LogSource.Volte, $"  B::::B     B:::::B oo:::::::::::oo  l::::lt:::::::::::::::::t      ee::::::::::::ee         ");
            Info(LogSource.Volte, $"  B::::BBBBBB:::::B o:::::::::::::::o l::::lt:::::::::::::::::t     e::::::eeeee:::::ee       ");
            Info(LogSource.Volte, $"  B:::::::::::::BB  o:::::ooooo:::::o l::::ltttttt:::::::tttttt    e::::::e     e:::::e       ");
            Info(LogSource.Volte, $"  B::::BBBBBB:::::B o::::o     o::::o l::::l      t:::::t          e:::::::eeeee::::::e       ");
            Info(LogSource.Volte, $"  B::::B     B:::::Bo::::o     o::::o l::::l      t:::::t          e:::::::::::::::::e        ");
            Info(LogSource.Volte, $"  B::::B     B:::::Bo::::o     o::::o l::::l      t:::::t          e::::::eeeeeeeeeee         ");
            Info(LogSource.Volte, $"  B::::B     B:::::Bo::::o     o::::o l::::l      t:::::t    tttttte:::::::e                  ");
            Info(LogSource.Volte, $"BB:::::BBBBBB::::::Bo:::::ooooo:::::ol::::::l     t::::::tttt:::::te::::::::e                 ");
            Info(LogSource.Volte, $"B:::::::::::::::::B o:::::::::::::::ol::::::l     tt::::::::::::::t e::::::::eeeeeeee         ");
            Info(LogSource.Volte, $"B::::::::::::::::B   oo:::::::::::oo l::::::l       tt:::::::::::tt  ee:::::::::::::e         ");
            Info(LogSource.Volte, $"BBBBBBBBBBBBBBBBB      ooooooooooo   llllllll         ttttttttttt      eeeeeeeeeeeeee         ");
            Info(LogSource.Volte, CommandsService.Separator.Trim());
            Info(LogSource.Volte, $"");
            Info(LogSource.Volte, $"Shamelessly stolen from Greem");
            Info(LogSource.Volte, $"Currently hijacking Volte V{Version.FullVersion}.");
            _headerPrinted = true;
            _headerPrinted = true;
        }

        private static void Log(LogSeverity s, LogSource from, string message, Exception e = null)
        {
            if (s is LogSeverity.Debug && !Config.EnableDebugLogging)
                return;

            sysoutLock.Lock(() => Execute(s, from, message, e));
        }

        /// <summary>
        ///     Prints a <see cref="LogSeverity.Debug"/> message to the console from the specified <paramref name="src"/> source, with the given <paramref name="message"/> message.
        /// </summary>
        /// <param name="src">Source to print the message from.</param>
        /// <param name="message">Message to print.</param>
        public static void Debug(LogSource src, string message) => Log(LogSeverity.Debug, src, message);

        /// <summary>
        ///     Prints an <see cref="LogSeverity.Info"/> message to the console from the specified <paramref name="src"/> source, with the given <paramref name="message"/> message.
        /// </summary>
        /// <param name="src">Source to print the message from.</param>
        /// <param name="message">Message to print.</param>
        public static void Info(LogSource src, string message) => Log(LogSeverity.Info, src, message);

        /// <summary>
        ///     Prints an <see cref="LogSeverity.Error"/> message to the console from the specified <paramref name="src"/> source, with the given <paramref name="message"/> message, with the specified <paramref name="e"/> exception if provided.
        /// </summary>
        /// <param name="src">Source to print the message from.</param>
        /// <param name="message">Message to print.</param>
        /// <param name="e">Optional Exception to print.</param>
        public static void Error(LogSource src, string message, Exception e = null) =>
            Log(LogSeverity.Error, src, message, e);

        /// <summary>
        ///     Prints an <see cref="LogSeverity.Error"/> message to the console from the specified <paramref name="src"/> source, with the given <paramref name="e"/> exception.
        /// </summary>
        /// <param name="src">Source to print the message from.</param>
        /// <param name="e">Exception to print.</param>
        public static void Error(LogSource src, Exception e) => Log(LogSeverity.Error, src, null, e);

        /// <summary>
        ///     Prints a <see cref="LogSeverity.Critical"/> message to the console from the specified <paramref name="src"/> source, with the given <paramref name="message"/> message, with the specified <paramref name="e"/> exception if provided.
        /// </summary>
        /// <param name="src">Source to print the message from.</param>
        /// <param name="message">Message to print.</param>
        /// <param name="e">Optional Exception to print.</param>
        public static void Critical(LogSource src, string message, Exception e = null) =>
            Log(LogSeverity.Critical, src, message, e);

        /// <summary>
        ///     Prints a <see cref="LogSeverity.Critical"/> message to the console from the specified <paramref name="src"/> source, with the given <paramref name="message"/> message, with the specified <paramref name="e"/> exception if provided.
        /// </summary>
        /// <param name="src">Source to print the message from.</param>
        /// <param name="message">Message to print.</param>
        /// <param name="e">Optional Exception to print.</param>
        public static void Warn(LogSource src, string message, Exception e = null) =>
            Log(LogSeverity.Warning, src, message, e);

        /// <summary>
        ///     Prints a <see cref="LogSeverity.Verbose"/> message to the console from the specified <paramref name="src"/> source, with the given <paramref name="message"/> message.
        /// </summary>
        /// <param name="src">Source to print the message from.</param>
        /// <param name="message">Message to print.</param>
        public static void Verbose(LogSource src, string message) => Log(LogSeverity.Verbose, src, message);

        /// <summary>
        ///     Prints an <see cref="LogSeverity.Error"/> message to the console from the specified <paramref name="e"/> exception.
        ///     This method calls <see cref="SentrySdk"/>'s CaptureException so it is logged to Sentry.
        /// </summary>
        /// <param name="e">Exception to print.</param>
        public static void Exception(Exception e) => Execute(LogSeverity.Error, LogSource.Volte, string.Empty, e);

        private static void Execute(LogSeverity s, LogSource src, string message, Exception e)
        {
            if (e is GatewayReconnectException)
            {
                Error(src, "Discord gateway requested reconnect.");
                return;
            }

            var content = new StringBuilder();
            var (color, value) = VerifySeverity(s);
            Append($"{value}:".PadRight(10), color);
            var dt = DateTime.Now.ToLocalTime();
            content.Append($"[{dt.FormatDate()} | {dt.FormatFullTime()}] {value} -> ");

            (color, value) = VerifySource(src);
            Append($"[{value}]".PadRight(10), color);
            content.Append($"{value} -> ");

            if (!message.IsNullOrWhitespace())
                Append(message, Color.White, ref content);

            if (e != null)
            {
                if (CaptureExceptionInSentry)
                    SentrySdk.CaptureException(e);

                var newlineOrEmpty = message.IsNullOrWhitespace() ? "" : Environment.NewLine;
                var toWrite = $"{newlineOrEmpty}{e.Message}{Environment.NewLine}{e.StackTrace}";
                Append(toWrite, Color.IndianRed, ref content);
            }

            Console.Write(Environment.NewLine);
            content.AppendLine();
            if (Config.EnabledFeatures.LogToFile)
                File.AppendAllText(NormalizeLogFilePath(DateTime.Now), content.ToString());
        }

        private static string NormalizeLogFilePath(DateTime date) =>
            _logFilePath.Replace("Volte", $"{date.Month}-{date.Day}-{date.Year}");

        private static void Append(string m, Color c)
        {
            Console.ForegroundColor = c;
            Console.Write(m);
        }

        private static void Append(string m, Color c, ref StringBuilder sb)
        {
            Console.ForegroundColor = c;
            Console.Write(m);
            sb.Append(m);
        }

        private static (Color Color, string Source) VerifySource(LogSource source) =>
            source switch
            {
                LogSource.Discord => (Color.RoyalBlue, "DISCORD"),
                LogSource.Gateway => (Color.RoyalBlue, "DISCORD"),
                LogSource.Volte => (Color.LawnGreen, "CORE"),
                LogSource.Service => (Color.Gold, "SERVICE"),
                LogSource.Module => (Color.LimeGreen, "MODULE"),
                LogSource.Rest => (Color.Red, "REST"),
                LogSource.Unknown => (Color.Fuchsia, "UNKNOWN"),
                _ => throw new InvalidOperationException($"The specified LogSource {source} is invalid.")
            };


        private static (Color Color, string Level) VerifySeverity(LogSeverity severity) =>
            severity switch
            {
                LogSeverity.Critical => (Color.Maroon, "CRITICAL"),
                LogSeverity.Error => (Color.DarkRed, "ERROR"),
                LogSeverity.Warning => (Color.Yellow, "WARN"),
                LogSeverity.Info => (Color.SpringGreen, "INFO"),
                LogSeverity.Verbose => (Color.Pink, "VERBOSE"),
                LogSeverity.Debug => (Color.SandyBrown, "DEBUG"),
                _ => throw new InvalidOperationException($"The specified LogSeverity ({severity}) is invalid.")
            };
    }
}