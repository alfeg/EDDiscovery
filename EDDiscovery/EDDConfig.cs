﻿using EDDiscovery.DB;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace EDDiscovery2
{
    public class EDDConfig
    {
        public class MapColoursClass
        {
            public System.Drawing.Color GetColour(string name)
            {
                return System.Drawing.Color.FromArgb(EDDConfig.Instance.GetSettingInt("MapColour_" + name));
            }

            public bool PutColour(string name, System.Drawing.Color colour)
            {
                return EDDConfig.Instance.PutSettingInt("MapColour_" + name, colour.ToArgb());
            }

            public System.Drawing.Color CoarseGridLines { get { return GetColour("CoarseGridLines"); } set { PutColour("CoarseGridLines", value); } }
            public System.Drawing.Color FineGridLines { get { return GetColour("FineGridLines"); } set { PutColour("FineGridLines", value); } }
            public System.Drawing.Color SystemDefault { get { return GetColour("SystemDefault"); } set { PutColour("SystemDefault", value); } }
            public System.Drawing.Color StationSystem { get { return GetColour("StationSystem"); } set { PutColour("StationSystem", value); } }
            public System.Drawing.Color CentredSystem { get { return GetColour("CentredSystem"); } set { PutColour("CentredSystem", value); } }
            public System.Drawing.Color SelectedSystem { get { return GetColour("SelectedSystem"); } set { PutColour("SelectedSystem", value); } }
            public System.Drawing.Color POISystem { get { return GetColour("POISystem"); } set { PutColour("POISystem", value); } }
            public System.Drawing.Color TrilatCurrentReference { get { return GetColour("TrilatCurrentReference"); } set { PutColour("TrilatCurrentReference", value); } }
            public System.Drawing.Color TrilatSuggestedReference { get { return GetColour("TrilatSuggestedReference"); } set { PutColour("TrilatSuggestedReference", value); } }
            public System.Drawing.Color PlannedRoute { get { return GetColour("PlannedRoute"); } set { PutColour("PlannedRoute", value); } }
            public System.Drawing.Color NamedStar { get { return GetColour("NamedStar"); } set { PutColour("NamedStar", value); } }
            public System.Drawing.Color NamedStarUnpopulated { get { return GetColour("NamedStarUnpop"); } set { PutColour("NamedStarUnpop", value); } }
        }

        private static EDDConfig _instance;
        public static EDDConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new EDDConfig();
                }
                return _instance;
            }
        }

        private bool _useDistances;
        private bool _EDSMLog;
        readonly public string LogIndex;
        private bool _canSkipSlowUpdates = false;
        private bool _orderrowsinverted = false;
        private bool _focusOnNewSystem = false; /**< Whether to automatically focus on a new system in the TravelHistory */
        private bool _keepOnTop = false; /**< Whether to keep the windows on top or not */
        private BindingList<EDCommander> _listCommanders = new BindingList<EDCommander>();
        public BindingList<EDCommander> listCommanders
        {
            get
            {
                if (_listCommanders.Count == 0)
                    Update();

                return _listCommanders;
            }
        }
        private int currentCmdrID=0;
        private Dictionary<string, object> settings = new Dictionary<string, object>();
        private Dictionary<string, Func<object>> defaults = new Dictionary<string, Func<object>>
        {
            { "Netlogdir", () => System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Frontier_Developments", "Products") },
            { "NetlogDirAutoMode", () => true },
            { "DefaultMap", () => System.Drawing.Color.Red.ToArgb() },
            { "MapColour_CoarseGridLines", () => System.Drawing.ColorTranslator.FromHtml("#296A6C").ToArgb() },
            { "MapColour_FineGridLines", () => System.Drawing.ColorTranslator.FromHtml("#202020").ToArgb() },
            { "MapColour_SystemDefault", () => System.Drawing.Color.White.ToArgb() },
            { "MapColour_StationSystem", () => System.Drawing.Color.RoyalBlue.ToArgb() },
            { "MapColour_CentredSystem", () => System.Drawing.Color.Yellow.ToArgb() },
            { "MapColour_SelectedSystem", () => System.Drawing.Color.Orange.ToArgb() },
            { "MapColour_POISystem", () => System.Drawing.Color.Purple.ToArgb() },
            { "MapColour_TrilatCurrentReference", () => System.Drawing.Color.Green.ToArgb() },
            { "MapColour_TrilatSuggestedReference", () => System.Drawing.Color.DarkOrange.ToArgb() },
            { "MapColour_PlannedRoute", () => System.Drawing.Color.Green.ToArgb() },
            { "MapColour_NamedStar", () => System.Drawing.Color.Yellow.ToArgb() },
            { "MapColour_NamedStarUnpop", () => System.Drawing.Color.FromArgb(255,192,192,0).ToArgb() }
        };

        private Dictionary<string, Action> onchange = new Dictionary<string, Action>
        {
            { "Netlogdir", () => { } },
            { "NetlogDirAutoMode", () => { } }
        };

        private EDDConfig()
        {
            LogIndex = DateTime.Now.ToString("yyyyMMdd");
        }

        public bool UseDistances
        {
            get
            {
                return _useDistances;
            }

            set
            {
                _useDistances = value;
                SQLiteDBClass.PutSettingBool("EDSMDistances", value);
            }
        }

        public int CurrentCmdrID
        {
            get
            {
                return CurrentCommander.Nr;
            }

            set
            {
                var cmdr = listCommanders.Select((c, i) => new { index = i, cmdr = c }).SingleOrDefault(a => a.cmdr.Nr == value);
                if (cmdr != null)
                {
                    currentCmdrID = cmdr.index;
                    SQLiteDBClass.PutSettingInt("ActiveCommander", value);
                }
            }
        }

        public EDCommander CurrentCommander
        {
            get
            {
                if (currentCmdrID >= listCommanders.Count)
                    currentCmdrID = listCommanders.Count - 1;


                return listCommanders[currentCmdrID];
            }
        }

        public bool EDSMLog
        {
            get
            {
                return _EDSMLog;
            }

            set
            {
                _EDSMLog = value;
                SQLiteDBClass.PutSettingBool("EDSMLog", value);
            }
        }

        public bool CanSkipSlowUpdates
        {
            get
            {
                return _canSkipSlowUpdates;
            }
            set
            {
                _canSkipSlowUpdates = value;
                SQLiteDBClass.PutSettingBool("CanSkipSlowUpdates", value);
            }
        }

        public bool OrderRowsInverted {
            get
            {
                return _orderrowsinverted;
            }
            set
            {
                _orderrowsinverted = value;
                SQLiteDBClass.PutSettingBool("OrderRowsInverted", value);
            }
        }

        public bool FocusOnNewSystem {
            get
            {
                return _focusOnNewSystem;
            }
            set
            {
                _focusOnNewSystem = value;
                SQLiteDBClass.PutSettingBool("FocusOnNewSystem", value);
            }
        }

        public bool KeepOnTop
        {
            get
            {
                return _keepOnTop;
            }
            set
            {
                _keepOnTop = value;
                SQLiteDBClass.PutSettingBool("KeepOnTop", value);
            }
        }

        public string NetLogDir { get { return GetSettingString("Netlogdir"); } set { PutSettingString("Netlogdir", value); } }
        public bool NetLogDirAutoMode { get { return GetSettingBool("NetlogDirAutoMode"); } set { PutSettingBool("NetlogDirAutoMode", value); } }
        public int DefaultMapColour { get { return GetSettingInt("DefaultMap"); } set { PutSettingInt("DefaultMap", value); } }
        public MapColoursClass MapColours { get; private set; } = new EDDConfig.MapColoursClass();

        public event Action NetLogDirChanged { add { onchange["Netlogdir"] += value; } remove { onchange["Netlogdir"] -= value; } }
        public event Action NetLogDirAutoModeChanged { add { onchange["NetlogDirAutoMode"] += value; } remove { onchange["NetlogDirAutoMode"] -= value; } }

        private bool GetSettingBool(string key)
        {
            return GetSetting<bool>(key, SQLiteDBClass.GetSettingBool);
        }

        private int GetSettingInt(string key)
        {
            return GetSetting<int>(key, SQLiteDBClass.GetSettingInt);
        }

        private double GetSettingDouble(string key)
        {
            return GetSetting<double>(key, SQLiteDBClass.GetSettingDouble);
        }

        private string GetSettingString(string key)
        {
            return GetSetting<string>(key, SQLiteDBClass.GetSettingString);
        }

        private T GetSetting<T>(string key, Func<string,T,T> getter)
        {
            if (!settings.ContainsKey(key))
            {
                T defval = default(T);
                if (defaults.ContainsKey(key))
                {
                    defval = (T)defaults[key]();
                }

                settings[key] = getter(key, defval);
            }

            return (T)settings[key];
        }

        private bool PutSettingBool(string key, bool value)
        {
            return PutSetting<bool>(key, value, SQLiteDBClass.PutSettingBool);
        }

        private bool PutSettingInt(string key, int value)
        {
            return PutSetting<int>(key, value, SQLiteDBClass.PutSettingInt);
        }

        private bool PutSettingDouble(string key, double value)
        {
            return PutSetting<double>(key, value, SQLiteDBClass.PutSettingDouble);
        }

        private bool PutSettingString(string key, string value)
        {
            return PutSetting<string>(key, value, SQLiteDBClass.PutSettingString);
        }

        private bool PutSetting<T>(string key, T value, Func<string,T,bool> setter)
        {
            settings[key] = value;

            if (setter(key, value))
            {
                if (onchange.ContainsKey(key))
                {
                    onchange[key]();
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        public void Update()
        {
            try
            {
                _useDistances = SQLiteDBClass.GetSettingBool("EDSMDistances", false);
                _EDSMLog = SQLiteDBClass.GetSettingBool("EDSMLog", false);
                _canSkipSlowUpdates = SQLiteDBClass.GetSettingBool("CanSkipSlowUpdates", false);
                _orderrowsinverted = SQLiteDBClass.GetSettingBool("OrderRowsInverted", false);
                _focusOnNewSystem = SQLiteDBClass.GetSettingBool("FocusOnNewSystem", false);
                _keepOnTop = SQLiteDBClass.GetSettingBool("KeepOnTop", false);
                LoadCommanders();

                if (_listCommanders.Count == 0)
                {
                    GetNewCommander();
                }

                int activecommander = SQLiteDBClass.GetSettingInt("ActiveCommander", 0);
                var cmdr = _listCommanders.Select((c, i) => new { index = i, cmdr = c }).SingleOrDefault(a => a.cmdr.Nr == activecommander);
                if (cmdr != null)
                {
                    currentCmdrID = cmdr.index;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("EDDConfig.Update()" + ":" + ex.Message);
                System.Diagnostics.Trace.WriteLine(ex.StackTrace);
            }

        }

        private void LoadCommanders()
        {
            _listCommanders.Clear();

            // Migrate old settigns.
            string apikey =  SQLiteDBClass.GetSettingString("EDSMApiKey", "");
            string commanderName =  SQLiteDBClass.GetSettingString("CommanderName", "");


            EDCommander cmdr = null;

            if (!SQLiteDBClass.GetSettingBool("EDCommanderDeleted0", false))
            {
                cmdr = new EDCommander(0, SQLiteDBClass.GetSettingString("EDCommanderName0", commanderName), SQLiteDBClass.GetSettingString("EDCommanderApiKey0", apikey));
                cmdr.NetLogPath = SQLiteDBClass.GetSettingString("EDCommanderNetLogPath0", null);
                _listCommanders.Add(cmdr);
            }

            for (int ii = 1; ii < 100; ii++)
            {
                bool deleted = SQLiteDBClass.GetSettingBool("EDCommanderDeleted" + ii.ToString(), false);
                if (!deleted)
                {
                    cmdr = new EDCommander(ii, SQLiteDBClass.GetSettingString("EDCommanderName" + ii.ToString(), ""), SQLiteDBClass.GetSettingString("EDCommanderApiKey" + ii.ToString(), ""));
                    cmdr.NetLogPath = SQLiteDBClass.GetSettingString("EDCommanderNetLogPath" + ii.ToString(), null);
                    if (!cmdr.Name.Equals(""))
                        _listCommanders.Add(cmdr);
                }
            }
        }

        public void StoreCommanders(IEnumerable<EDCommander> dictcmdr)
        {
            foreach (EDCommander cmdr in dictcmdr)
            {
                SQLiteDBClass.PutSettingString("EDCommanderName" + cmdr.Nr.ToString(), cmdr.Name);
                SQLiteDBClass.PutSettingString("EDCommanderApiKey" + cmdr.Nr.ToString(), cmdr.APIKey);
                SQLiteDBClass.PutSettingString("EDCommanderNetLogPath" + cmdr.Nr.ToString(), cmdr.NetLogPath);
            }

            LoadCommanders();
        }

        internal EDCommander GetNewCommander()
        {
            int maxnr = 0;
            foreach (EDCommander _cmdr in _listCommanders)
            {
                maxnr = Math.Max(_cmdr.Nr, maxnr);
            }

            maxnr++;

            if (SQLiteDBClass.GetSettingBool("EDCommanderDeleted" + maxnr.ToString(), false))
            {
                SQLiteDBClass.PutSettingBool("EDCommanderDeleted" + maxnr.ToString(), false);
            }

            var cmdr = new EDCommander(maxnr, "CMDR "+maxnr.ToString(), "");

            _listCommanders.Add(cmdr);

            SQLiteDBClass.PutSettingString("EDCommanderName" + cmdr.Nr.ToString(), cmdr.Name);
            SQLiteDBClass.PutSettingString("EDCommanderApiKey" + cmdr.Nr.ToString(), cmdr.APIKey);
            SQLiteDBClass.PutSettingString("EDCommanderNetLogPath" + cmdr.Nr.ToString(), cmdr.NetLogPath);

            LoadCommanders();

            return cmdr;
        }

        public void DeleteCommander(EDCommander cmdr)
        {
            SQLiteDBClass.PutSettingBool("EDCommanderDeleted" + cmdr.Nr.ToString(), true);

            LoadCommanders();
        }
    }
}
