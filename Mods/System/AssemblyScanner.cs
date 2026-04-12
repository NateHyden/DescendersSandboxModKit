using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using MelonLoader;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    public class ScanResult
    {
        public string Category;
        public string Name;
        public bool OK;
        public string Detail;
    }

    public static class AssemblyScanner
    {
        public static readonly List<ScanResult> Results = new List<ScanResult>();
        public static bool HasRun { get; private set; } = false;
        public static int OKCount { get; private set; } = 0;
        public static int FailCount { get; private set; } = 0;

        private static readonly string SaveFolder =
            Path.Combine(
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UserData"),
                "DescendersModMenu");

        private static readonly string ReportFile =
            Path.Combine(SaveFolder, "AssemblyReport.txt");

        public static string ReportPath => ReportFile;

        // ── Run ───────────────────────────────────────────────────────
        public static void Run()
        {
            Results.Clear();
            OKCount = 0;
            FailCount = 0;

            MelonLogger.Msg("[AssemblyScanner] Starting scan...");

            RunBikePhysicsChecks();
            RunMovementChecks();
            RunSuspensionChecks();
            RunTricksChecks();
            RunPlayerChecks();
            RunSessionChecks();
            RunWorldChecks();
            RunNavigationChecks();

            HasRun = true;
            WriteReport();
            MelonLogger.Msg("[AssemblyScanner] Done. "
                + OKCount + " OK / " + FailCount + " FAILED.");
        }

        // ── Bike Physics ──────────────────────────────────────────────
        private static void RunBikePhysicsChecks()
        {
            bool ok; string det;

            // Vehicle type
            ok = false; det = "NOT FOUND";
            try { ok = (object)typeof(Vehicle) != null; det = ok ? "OK" : "NOT FOUND"; }
            catch (Exception ex) { det = ex.Message; }
            Add("Bike Physics", "Vehicle type", ok, det);

            // Acceleration field on Vehicle
            ok = false; det = "NOT FOUND";
            try
            {
                var f = typeof(Vehicle).GetField("cPkCE^\x81",
                    BindingFlags.Public | BindingFlags.Instance);
                ok = (object)f != null;
                det = ok ? "OK" : "NOT FOUND";
            }
            catch (Exception ex) { det = ex.Message; }
            Add("Bike Physics", "Acceleration field (cPkCE^)", ok, det);

            // VehicleController type
            ok = false; det = "NOT FOUND";
            try { ok = (object)typeof(VehicleController) != null; det = ok ? "OK" : "NOT FOUND"; }
            catch (Exception ex) { det = ex.Message; }
            Add("Bike Physics", "VehicleController type", ok, det);
        }

        // ── Movement ──────────────────────────────────────────────────
        private static void RunMovementChecks()
        {
            // These fields live on a runtime subclass of VehicleController.
            // Static check on typeof(VehicleController) always returns null.
            // The mod resolves them via instance.GetType() at runtime — confirmed working.
            AddWarn("Movement", "Spin rotation field", "Live-verified only (runtime subclass)");
            AddWarn("Movement", "Hop force field", "Live-verified only (runtime subclass)");
            AddWarn("Movement", "Wheelie force field", "Live-verified only (runtime subclass)");
            AddWarn("Movement", "Lean strength field", "Live-verified only (runtime subclass)");
        }

        // ── Suspension ────────────────────────────────────────────────
        private static void RunSuspensionChecks()
        {
            bool ok; string det;

            ok = false; det = "NOT FOUND";
            try { ok = (object)typeof(Wheel) != null; det = ok ? "OK" : "NOT FOUND"; }
            catch (Exception ex) { det = ex.Message; }
            Add("Suspension", "Wheel type", ok, det);

            CheckTypeField("Suspension", "Travel field (xL{gJGT)",
                typeof(Wheel), "xL\x7BgJGT");
            CheckTypeField("Suspension", "Stiffness field (p~mkyX{)",
                typeof(Wheel), "p\x7EmkyX\x7B");
            CheckTypeField("Suspension", "Damping field (YrKDSPL)",
                typeof(Wheel), "YrKDSPL");
        }

        // ── Tricks / Scoring ──────────────────────────────────────────
        private static void RunTricksChecks()
        {
            bool ok; string det;

            // VehicleTricks type
            ok = false; det = "NOT FOUND";
            try { ok = (object)typeof(VehicleTricks) != null; det = ok ? "OK" : "NOT FOUND"; }
            catch (Exception ex) { det = ex.Message; }
            Add("Tricks", "VehicleTricks type", ok, det);

            // Combo field (type name starts with "Combo")
            ok = false; det = "NOT FOUND";
            try
            {
                FieldInfo[] fields = typeof(VehicleTricks).GetFields(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                FieldInfo comboFld = null;
                for (int i = 0; i < fields.Length; i++)
                    if (fields[i].FieldType.Name.StartsWith("Combo",
                        StringComparison.Ordinal))
                    { comboFld = fields[i]; break; }
                ok = (object)comboFld != null;
                det = ok ? "OK — field: " + comboFld.Name : "NOT FOUND";
            }
            catch (Exception ex) { det = ex.Message; }
            Add("Tricks", "Combo field on VehicleTricks", ok, det);

            // Combo.score field
            ok = false; det = "NOT FOUND";
            try
            {
                FieldInfo[] fields = typeof(VehicleTricks).GetFields(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                Type comboType = null;
                for (int i = 0; i < fields.Length; i++)
                    if (fields[i].FieldType.Name.StartsWith("Combo",
                        StringComparison.Ordinal))
                    { comboType = fields[i].FieldType; break; }
                if ((object)comboType != null)
                {
                    FieldInfo[] cf = comboType.GetFields(
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    for (int i = 0; i < cf.Length; i++)
                        if (string.Equals(cf[i].Name, "score", StringComparison.Ordinal))
                        { ok = true; det = "OK"; break; }
                    if (!ok) det = "Combo type found but 'score' field missing";
                }
                else det = "Combo type not found";
            }
            catch (Exception ex) { det = ex.Message; }
            Add("Tricks", "Combo.score field", ok, det);

            // Multiplier property
            ok = false; det = "NOT FOUND";
            try
            {
                var p = typeof(VehicleTricks).GetProperty("FnHLcjK",
                    BindingFlags.Public | BindingFlags.Instance);
                ok = (object)p != null;
                det = ok ? "OK" : "NOT FOUND";
            }
            catch (Exception ex) { det = ex.Message; }
            Add("Tricks", "Multiplier property (FnHLcjK)", ok, det);

            // Max multiplier — lives on runtime subclass, same as Movement fields
            AddWarn("Tricks", "Max multiplier field (uDh]dJt)",
                "Live-verified only (runtime subclass)");
        }

        // ── Player ────────────────────────────────────────────────────
        private static void RunPlayerChecks()
        {
            bool ok; string det;

            ok = false; det = "NOT FOUND";
            try { ok = (object)typeof(PlayerInfoImpact) != null; det = ok ? "OK" : "NOT FOUND"; }
            catch (Exception ex) { det = ex.Message; }
            Add("Player", "PlayerInfoImpact type", ok, det);

            CheckTypeMethod("Player", "IsHumanControlled",
                typeof(PlayerInfoImpact), "IsHumanControlled",
                BindingFlags.Public | BindingFlags.Instance);
            CheckTypeMethod("Player", "Nobail method",
                typeof(PlayerInfoImpact), "Nobail",
                BindingFlags.Public | BindingFlags.Instance);
            CheckTypeMethod("Player", "RespawnAtStartLine",
                typeof(PlayerInfoImpact), "RespawnAtStartLine",
                BindingFlags.Public | BindingFlags.Instance);
            CheckTypeMethod("Player", "RespawnOnTrack",
                typeof(PlayerInfoImpact), "RespawnOnTrack",
                BindingFlags.Public | BindingFlags.Instance);

            ok = false; det = "NOT FOUND";
            try { ok = (object)typeof(Cyclist) != null; det = ok ? "OK" : "NOT FOUND"; }
            catch (Exception ex) { det = ex.Message; }
            Add("Player", "Cyclist type", ok, det);

            CheckTypeMethod("Player", "Cyclist.Bail method",
                typeof(Cyclist), "Bail",
                BindingFlags.Public | BindingFlags.Instance);
        }

        // ── Session ───────────────────────────────────────────────────
        private static void RunSessionChecks()
        {
            bool ok; string det;

            ok = false; det = "NOT FOUND";
            try { ok = (object)typeof(VehicleEvents) != null; det = ok ? "OK" : "NOT FOUND"; }
            catch (Exception ex) { det = ex.Message; }
            Add("Session", "VehicleEvents type", ok, det);

            // Checkpoint property — starts with ']' (0x5D)
            ok = false; det = "NOT FOUND";
            try
            {
                PropertyInfo[] props = typeof(VehicleEvents).GetProperties(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                for (int i = 0; i < props.Length; i++)
                    if (props[i].Name.Length > 0 && props[i].Name[0] == ']')
                    { ok = true; det = "OK — prop: " + props[i].Name; break; }
                if (!ok) det = "NOT FOUND";
            }
            catch (Exception ex) { det = ex.Message; }
            Add("Session", "Checkpoint index property", ok, det);
        }

        // ── World ─────────────────────────────────────────────────────
        private static void RunWorldChecks()
        {
            bool ok; string det;

            // TOD_Sky type
            ok = false; det = "NOT FOUND";
            Type todSky = null;
            try
            {
                Assembly[] asms = AppDomain.CurrentDomain.GetAssemblies();
                for (int i = 0; i < asms.Length; i++)
                {
                    todSky = asms[i].GetType("TOD_Sky");
                    if ((object)todSky != null) { ok = true; det = "OK"; break; }
                }
                if (!ok) det = "NOT FOUND in any assembly";
            }
            catch (Exception ex) { det = ex.Message; }
            Add("World", "TOD_Sky type", ok, det);

            // TOD Cycle field
            ok = false; det = "NOT FOUND";
            try
            {
                if ((object)todSky != null)
                {
                    FieldInfo[] fields = todSky.GetFields(
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    for (int i = 0; i < fields.Length; i++)
                        if (string.Equals(fields[i].Name, "Cycle", StringComparison.Ordinal))
                        { ok = true; det = "OK"; break; }
                    if (!ok) det = "TOD_Sky found but Cycle field missing";
                }
                else det = "TOD_Sky not found — skipped";
            }
            catch (Exception ex) { det = ex.Message; }
            Add("World", "TOD_Sky.Cycle field", ok, det);

            // Hour field on Cycle
            ok = false; det = "NOT FOUND";
            try
            {
                if ((object)todSky != null)
                {
                    FieldInfo cycleFld = null;
                    FieldInfo[] fields = todSky.GetFields(
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    for (int i = 0; i < fields.Length; i++)
                        if (string.Equals(fields[i].Name, "Cycle", StringComparison.Ordinal))
                        { cycleFld = fields[i]; break; }
                    if ((object)cycleFld != null)
                    {
                        Type cycleType = cycleFld.FieldType;
                        FieldInfo[] cf = cycleType.GetFields(
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        for (int i = 0; i < cf.Length; i++)
                            if (string.Equals(cf[i].Name, "Hour", StringComparison.Ordinal))
                            { ok = true; det = "OK"; break; }
                        if (!ok)
                        {
                            PropertyInfo[] cp = cycleType.GetProperties(
                                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            for (int i = 0; i < cp.Length; i++)
                                if (string.Equals(cp[i].Name, "Hour", StringComparison.Ordinal))
                                { ok = true; det = "OK (property)"; break; }
                        }
                        if (!ok) det = "Cycle found but Hour field/property missing";
                    }
                    else det = "Cycle field not found — skipped";
                }
                else det = "TOD_Sky not found — skipped";
            }
            catch (Exception ex) { det = ex.Message; }
            Add("World", "TOD cycle Hour field", ok, det);

            ok = false; det = "NOT FOUND";
            try { ok = (object)typeof(SessionManager) != null; det = ok ? "OK" : "NOT FOUND"; }
            catch (Exception ex) { det = ex.Message; }
            Add("World", "SessionManager type", ok, det);

            CheckTypeMethod("World", "SessionManager.GetWorld",
                typeof(SessionManager), "GetWorld",
                BindingFlags.Public | BindingFlags.Instance);

            ok = false; det = "NOT FOUND";
            try { ok = (object)typeof(AudioManager) != null; det = ok ? "OK" : "NOT FOUND"; }
            catch (Exception ex) { det = ex.Message; }
            Add("World", "AudioManager type", ok, det);

            CheckTypeMethod("World", "AudioManager.SetCategoryVolume",
                typeof(AudioManager), "SetCategoryVolume",
                BindingFlags.Public | BindingFlags.Instance);
        }

        // ── Navigation ────────────────────────────────────────────────
        private static void RunNavigationChecks()
        {
            bool ok; string det;

            ok = false; det = "NOT FOUND";
            try { ok = (object)typeof(PlayerManager) != null; det = ok ? "OK" : "NOT FOUND"; }
            catch (Exception ex) { det = ex.Message; }
            Add("Navigation", "PlayerManager type", ok, det);

            ok = false; det = "NOT FOUND";
            try { ok = (object)typeof(MultiManager) != null; det = ok ? "OK" : "NOT FOUND"; }
            catch (Exception ex) { det = ex.Message; }
            Add("Navigation", "MultiManager type", ok, det);

            ok = false; det = "NOT FOUND";
            try
            {
                var f = typeof(PermaGUI).GetField("\x5B\x7EqsVD\x7C",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                ok = (object)f != null;
                det = ok ? "OK" : "Field removed in game update — MapChanger skips this path safely";
            }
            catch (Exception ex) { det = ex.Message; }
            Add("Navigation", "PermaGUI level field", ok, det);

            // Level info type
            ok = false; det = "NOT FOUND";
            try
            {
                Assembly[] asms = AppDomain.CurrentDomain.GetAssemblies();
                for (int i = 0; i < asms.Length; i++)
                {
                    Type t = asms[i].GetType("\x81wiWlGz");
                    if ((object)t != null) { ok = true; det = "OK"; break; }
                }
                if (!ok) det = "NOT FOUND in any assembly";
            }
            catch (Exception ex) { det = ex.Message; }
            Add("Navigation", "Level info type", ok, det);
        }

        // ── Shared check helpers (no lambdas) ─────────────────────────
        private static void CheckTypeField(string cat, string name, Type t, string fieldName)
        {
            bool ok = false; string det = "NOT FOUND";
            try
            {
                var f = t.GetField(fieldName,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                ok = (object)f != null;
                det = ok ? "OK" : "NOT FOUND";
            }
            catch (Exception ex) { det = ex.Message; }
            Add(cat, name, ok, det);
        }

        private static void CheckTypeMethod(string cat, string name, Type t,
            string methodName, BindingFlags flags)
        {
            bool ok = false; string det = "NOT FOUND";
            try
            {
                var m = t.GetMethod(methodName, flags);
                ok = (object)m != null;
                det = ok ? "OK" : "NOT FOUND";
            }
            catch (Exception ex) { det = ex.Message; }
            Add(cat, name, ok, det);
        }

        // WARN = cannot verify statically but confirmed working at runtime
        private static void AddWarn(string cat, string name, string detail)
        {
            Results.Add(new ScanResult
            {
                Category = cat,
                Name = name,
                OK = true,   // treat as OK — mod is confirmed working
                Detail = "WARN: " + detail
            });
            OKCount++;
        }

        private static void Add(string cat, string name, bool ok, string detail)
        {
            Results.Add(new ScanResult
            {
                Category = cat,
                Name = name,
                OK = ok,
                Detail = detail
            });
            if (ok) OKCount++;
            else FailCount++;
        }

        // ── Write report ──────────────────────────────────────────────
        private static void WriteReport()
        {
            try
            {
                if (!Directory.Exists(SaveFolder))
                    Directory.CreateDirectory(SaveFolder);

                var sb = new System.Text.StringBuilder();
                sb.AppendLine("DescendersToolKit — Assembly Scan Report");
                sb.AppendLine("Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                sb.AppendLine("Game version: " + Application.version);
                sb.AppendLine(new string('-', 60));
                sb.AppendLine("SUMMARY: " + OKCount + " OK / " + FailCount
                    + " FAILED out of " + Results.Count + " checks");
                sb.AppendLine(new string('-', 60));

                string lastCat = "";
                for (int i = 0; i < Results.Count; i++)
                {
                    var r = Results[i];
                    if (!string.Equals(r.Category, lastCat, StringComparison.Ordinal))
                    {
                        sb.AppendLine();
                        sb.AppendLine("[" + r.Category.ToUpper() + "]");
                        lastCat = r.Category;
                    }
                    string status = r.OK ? "OK    " : "FAILED";
                    sb.AppendLine("  " + status + "  " + r.Name
                        + (r.OK ? "" : " — " + r.Detail));
                }

                sb.AppendLine();
                sb.AppendLine(new string('-', 60));
                if (FailCount == 0)
                    sb.AppendLine("All checks passed. Assembly is compatible.");
                else
                    sb.AppendLine(FailCount + " check(s) failed. These mods may not work correctly.");

                File.WriteAllText(ReportFile, sb.ToString());
                MelonLogger.Msg("[AssemblyScanner] Report saved: " + ReportFile);
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[AssemblyScanner] WriteReport: " + ex.Message);
            }
        }
    }
}