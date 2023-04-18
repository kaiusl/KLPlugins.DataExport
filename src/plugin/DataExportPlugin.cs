using System;
using System.IO;
using System.Runtime.CompilerServices;
using GameReaderCommon;
using SimHub.Plugins;

namespace KLPlugins.DataExport
{
    [PluginDescription("")]
    [PluginAuthor("Kaius Loos")]
    [PluginName("DataExportPlugin")]
    public class DataExport : IPlugin, IDataPlugin
    {
        public PluginManager PluginManager { get; set; }
        //public ImageSource PictureIcon => this.ToIcon(Properties.Resources.sdkmenuicon);
        public string LeftMenuTitle => PluginName;

        internal const string PluginName = "DataExport";
        internal static Game Game; // Const during the lifetime of this plugin, plugin is rebuilt at game change
        internal static string GameDataPath; // Same as above
        internal static string PluginStartTime = $"{DateTime.Now.ToString("dd-MM-yyyy_HH-mm-ss")}";
        internal static PluginManager PManager;

        private static FileStream _logFile;
        private static StreamWriter _logWriter;
        private static bool _isLogFlushed = false;
        private string LogFileName;
        private FileStream _timingFile;
        private StreamWriter _timingWriter;
        private Values _values;

        /// <summary>
        /// Called one time per game data update, contains all normalized game data, 
        /// raw data are intentionally "hidden" under a generic object type (A plugin SHOULD NOT USE IT)
        /// 
        /// This method is on the critical path, it must execute as fast as possible and avoid throwing any error
        /// 
        /// </summary>
        /// <param name="pluginManager"></param>
        /// <param name="data"></param>
        public void DataUpdate(PluginManager pm, ref GameData data)
        {
            if (!Game.IsAcc) { return; } // Atm only ACC is supported

            if (data.GameRunning && data.OldData != null && data.NewData != null)
            {
                //WriteFrameTimes(pm);
                _values.OnDataUpdate(pm, data);
            }
        }

        private void WriteFrameTimes(PluginManager pm)
        {
            var ftime = (double)pm.GetPropertyValue<SimHub.Plugins.DataPlugins.DataCore.DataCorePlugin>("Performance_FrameDuration");
            var cached = (double)pm.GetPropertyValue<SimHub.Plugins.DataPlugins.DataCore.DataCorePlugin>("Performance_CachedFormulasPerSecond");
            var jsFormulas = (double)pm.GetPropertyValue<SimHub.Plugins.DataPlugins.DataCore.DataCorePlugin>("Performance_JSFormulasPerSecond");
            var NALCFormulas = (double)pm.GetPropertyValue<SimHub.Plugins.DataPlugins.DataCore.DataCorePlugin>("Performance_NALCFormulasPerSecond");
            var NALCOptFormulas = (double)pm.GetPropertyValue<SimHub.Plugins.DataPlugins.DataCore.DataCorePlugin>("Performance_NALCOptimizedFormulasPerSecond");

            if (_timingWriter != null)
            {
                _timingWriter.WriteLine($"{ftime};{cached};{jsFormulas};{NALCFormulas};{NALCOptFormulas}");
            }
        }

        /// <summary>
        /// Called at plugin manager stop, close/dispose anything needed here !
        /// Plugins are rebuilt at game change
        /// </summary>
        /// <param name="pluginManager"></param>
        public void End(PluginManager pluginManager)
        {
            //this.SaveCommonSettings("GeneralSettings", Settings);
            if (_values != null)
            {
                _values.Dispose();
            }
            if (_logWriter != null)
            {
                _logWriter.Dispose();
                _logWriter = null;
            }
            if (_logFile != null)
            {
                _logFile.Dispose();
                _logFile = null;
            }

            if (_timingWriter != null)
            {
                _timingWriter.Dispose();
                _timingWriter = null;
            }

            if (_timingFile != null)
            {
                _timingFile.Dispose();
                _timingFile = null;
            }
        }

        /// <summary>
        /// Returns the settings control, return null if no settings control is required
        /// </summary>
        /// <param name="pluginManager"></param>
        /// <returns></returns>
        public System.Windows.Controls.Control GetWPFSettingsControl(PluginManager pluginManager)
        {
            return null;//new SettingsControl(this);
        }

        /// <summary>
        /// Called once after plugins startup
        /// Plugins are rebuilt at game change
        /// </summary>
        /// <param name="pluginManager"></param>
        public void Init(PluginManager pluginManager)
        {
            LogFileName = $"PluginsData\\KLPlugins\\DataExport\\Logs\\Log_{PluginStartTime}.txt";
            var gameName = (string)pluginManager.GetPropertyValue<SimHub.Plugins.DataPlugins.DataCore.DataCorePlugin>("CurrentGame");
            Game = new Game(gameName);
            if (!Game.IsAcc) return;
            PManager = pluginManager;
            _values = new Values();
            PManager.GameStateChanged += _values.OnGameStateChanged;
            InitLogginig();
        }

        internal void InitLogginig()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LogFileName));
            _logFile = File.Create(LogFileName);
            _logWriter = new StreamWriter(_logFile);
        }

        internal static void LogInfo(string msg, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            Log(msg, memberName, sourceFilePath, lineNumber, "INFO", SimHub.Logging.Current.Info);
        }

        internal static void LogWarn(string msg, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            Log(msg, memberName, sourceFilePath, lineNumber, "WARN", SimHub.Logging.Current.Warn);
        }

        internal static void LogError(string msg, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            Log(msg, memberName, sourceFilePath, lineNumber, "ERROR", SimHub.Logging.Current.Error);
        }


        private static void Log(string msg, string memberName, string sourceFilePath, int lineNumber, string lvl, Action<string> simHubLog)
        {
            var pathParts = sourceFilePath.Split('\\');
            var m = CreateMessage(msg, pathParts[pathParts.Length - 1], memberName, lineNumber);
            simHubLog($"{PluginName} {m}");
            LogToFile($"{DateTime.Now.ToString("dd.MM.yyyy HH:mm.ss")} {lvl.ToUpper()} {m}\n");
        }

        private static string CreateMessage(string msg, string source, string memberName, int lineNumber)
        {
            return $"({source}: {memberName},{lineNumber})\n\t{msg}";
        }

        internal static void LogFileSeparator()
        {
            LogToFile("\n----------------------------------------------------------\n");
        }


        private static void LogToFile(string msq)
        {
            if (_logWriter != null)
            {
                _logWriter.WriteLine(msq);
                _isLogFlushed = false;
            }
        }
    }
}