﻿using System.Collections.Generic;
using System.Data;
using NLog;

namespace Plus.Core.Settings;

public class SettingsManager
{
    private static readonly ILogger Log = LogManager.GetLogger("Plus.Core.Settings.SettingsManager");
    private readonly Dictionary<string, string> _settings;

    public SettingsManager()
    {
        _settings = new Dictionary<string, string>();
    }

    public void Init()
    {
        if (_settings.Count > 0)
            _settings.Clear();
        using (var dbClient = PlusEnvironment.GetDatabaseManager().GetQueryReactor())
        {
            dbClient.SetQuery("SELECT * FROM `server_settings`");
            var table = dbClient.GetTable();
            if (table != null)
                foreach (DataRow row in table.Rows)
                    _settings.Add(row["key"].ToString().ToLower(), row["value"].ToString().ToLower());
        }
        Log.Info("Loaded " + _settings.Count + " server settings.");
    }

    public string TryGetValue(string value) => _settings.ContainsKey(value) ? _settings[value] : "0";
}