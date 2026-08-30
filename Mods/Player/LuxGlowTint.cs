using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using MelonLoader;
using UnityEngine;
using DescendersModMenu;

namespace DescendersModMenu.Mods
{
    /// <summary>
    /// Per-slot Lux glow colours via vanilla HueAnimate-style HDR material emission (bloom).
    /// Saves equipped item id before Lux swap; restores on disable.
    /// </summary>
    public static class LuxGlowTint
    {
        public enum Part
        {
            Head = 1,
            Torso = 2,
            Legs = 3,
            Bike = 4,
            Eyes = 7
        }

        public static readonly Part[] AllParts = {
            Part.Bike, Part.Head, Part.Torso, Part.Legs, Part.Eyes
        };

        private const int HueStepDegrees = 4;
        private const float LuxBaseHdrV = 2.5f;
        private const float RainbowHueSpeedPerLevel = 0.028f;
        private const int DefaultBrightnessLevel = 10;
        private const int MinBrightnessLevel = 1;
        private const int MaxBrightnessLevel = 40;
        private const int BrightnessPercentStep = 10;
        private const int DefaultRainbowSpeedLevel = 10;
        private const int MinRainbowSpeedLevel = 1;
        private const int MaxRainbowSpeedLevel = 40;
        private const int LuxCustomGearMinId = 1514;
        private const int LuxCustomGearMaxId = 1598;
        private const int ExtraordinaryRarity = 3;
        private const int NoSavedItem = -1;
        private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        private struct GlowTarget
        {
            public Renderer Renderer;
            public HueAnimate Hue;
            public bool UseMaterialGlow;
        }

        private static readonly bool[] _enabled = new bool[15];
        private static readonly float[] _hue = new float[15];
        private static readonly float[] _saturation = new float[15];
        private static readonly int[] _brightness = new int[15];
        private static readonly int[] _savedItemId = new int[15];
        private static readonly int[] _pendingRestoreId = new int[15];
        private static readonly int[] _equipGen = new int[15];
        private static readonly List<GlowTarget>[] _activeByPart = new List<GlowTarget>[15];
        private static readonly List<GlowTarget> _scratchTargets = new List<GlowTarget>(32);

        private static FieldInfo _haSpeed;
        private static FieldInfo _haPhaseOffset;
        private static FieldInfo _haHueAccum;
        private static readonly Dictionary<HueAnimate, float> _haOriginalSpeed
            = new Dictionary<HueAnimate, float>();
        private static readonly HashSet<int> _frozenHueIds
            = new HashSet<int>();
        private static readonly Dictionary<int, Material[]> _originalSharedMats
            = new Dictionary<int, Material[]>();
        private static float _forceRebuildUntil;
        private static int _lastPcInstanceId;
        private static bool _rainbowAll;
        private static float _rainbowHue;
        private static int _rainbowSpeedLevel = DefaultRainbowSpeedLevel;

        private static MethodInfo _refreshBikeMesh;

        private static Type _slotEnumType;
        private static MethodInfo _getItemInstance;
        private static MethodInfo _equipItem;
        private static MethodInfo _equipItemRoutine;
        private static MethodInfo _unequipItem;
        private static MethodInfo _getItemsOfSlot;
        private static MethodInfo _getLinkForSlot;
        private static MethodInfo _isCustomGear;
        private static FieldInfo _linksField;
        private static FieldInfo _linkSpawnParent;
        private static FieldInfo _itemInstanceItem;
        private static FieldInfo _itemInstancePrefab;
        private static bool _refsResolved;
        private static int _luxTick;
        private static PlayerCustomization _pcCache;
        private static MelonPreferences_Category _savedCat;
        private static readonly MelonPreferences_Entry<int>[] _savedEntries
            = new MelonPreferences_Entry<int>[15];
        private static MelonPreferences_Entry<bool> _pendingVanillaEntry;
        private static bool _restoredKnownItem;
        private static int _bootRepairGen;

        static LuxGlowTint()
        {
            for (int i = 0; i < _savedItemId.Length; i++)
            {
                _savedItemId[i] = NoSavedItem;
                _pendingRestoreId[i] = NoSavedItem;
            }

            _hue[(int)Part.Bike] = 0.52f;
            _hue[(int)Part.Head] = 0.88f;
            _hue[(int)Part.Torso] = 0.17f;
            _hue[(int)Part.Legs] = 0.06f;
            _hue[(int)Part.Eyes] = 0.47f;
            for (int i = 0; i < _brightness.Length; i++)
            {
                _brightness[i] = DefaultBrightnessLevel;
                _saturation[i] = 1f;
            }
        }

        public static void Init()
        {
            _savedCat = MelonPreferences.CreateCategory("LuxGlowSavedItems", "Lux Glow previous gear");
            _pendingVanillaEntry = _savedCat.CreateEntry<bool>(
                "PendingVanillaRestore", false,
                "Restore pre-Lux gear once after quit with Lux on");
            for (int p = 0; p < AllParts.Length; p++)
            {
                Part part = AllParts[p];
                int i = (int)part;
                _savedEntries[i] = _savedCat.CreateEntry<int>(
                    "Prev_" + PartLabel(part), NoSavedItem,
                    "Item id worn before Lux " + PartLabel(part));
                int fromDisk = _savedEntries[i].Value;
                if (fromDisk != NoSavedItem)
                    _savedItemId[i] = fromDisk;
            }
            ModLog.Debug("[LuxTint] Loaded previous-gear ids from preferences.");
        }

        private static void PersistSavedId(Part part)
        {
            int i = (int)part;
            if ((object)_savedEntries[i] == null) return;
            _savedEntries[i].Value = _savedItemId[i];
            MelonPreferences.Save();
        }

        public static float GetHue01(Part part) => _hue[(int)part];

        public static float GetSaturation(Part part) => _saturation[(int)part];

        public static int GetSaturationPercent(Part part)
        {
            return (int)(GetSaturation(part) * 100f);
        }

        public static float GetBrightnessLuminance01(Part part)
        {
            float t = (float)(_brightness[(int)part] - MinBrightnessLevel)
                / (float)(MaxBrightnessLevel - MinBrightnessLevel);
            return Mathf.Clamp01(t);
        }

        public static void SetBrightnessLuminance01(Part part, float lum01)
        {
            lum01 = Mathf.Clamp01(lum01);
            int level = MinBrightnessLevel
                + (int)(lum01 * (MaxBrightnessLevel - MinBrightnessLevel));
            _brightness[(int)part] = Mathf.Clamp(level, MinBrightnessLevel, MaxBrightnessLevel);
        }

        public static void ApplyPickerSelection(Part part, float hue01, float saturation, float luminance01)
        {
            _rainbowAll = false;
            int i = (int)part;
            _hue[i] = Mathf.Repeat(hue01, 1f);
            _saturation[i] = Mathf.Clamp01(saturation);
            SetBrightnessLuminance01(part, luminance01);

            if (!_enabled[i])
            {
                _enabled[i] = true;
                CaptureItemBeforeLux(part);
                EnablePart(part);
            }
            else
            {
                ApplyGlow(part);
                ScheduleRefresh(part);
            }
        }

        public static int GetBrightnessPercent(Part part)
        {
            return _brightness[(int)part] * BrightnessPercentStep;
        }

        public static void StepBrightness(Part part, int delta)
        {
            int i = (int)part;
            int next = _brightness[i] + delta;
            if (next < MinBrightnessLevel) next = MinBrightnessLevel;
            if (next > MaxBrightnessLevel) next = MaxBrightnessLevel;
            _brightness[i] = next;

            if (_enabled[i])
            {
                ApplyGlow(part);
                ScheduleRefresh(part);
            }

            ModLog.Feedback("[LuxTint] " + PartLabel(part) + " brightness -> " + GetBrightnessPercent(part) + "%");
        }

        private static float GetHdrV(Part part)
        {
            return LuxBaseHdrV * (GetBrightnessPercent(part) / 100f);
        }

        public static bool TryParseHueInput(string input, out int degrees)
        {
            degrees = 0;
            if (input == null) return false;
            string s = input.Trim();
            if (s.Length == 0) return false;
            if (s.Length > 0 && (s[0] == 'H' || s[0] == 'h'))
                s = s.Substring(1).Trim();
            if (!int.TryParse(s, out degrees)) return false;
            if (degrees < 0 || degrees > 359) return false;
            return true;
        }

        public static bool ApplyHueInput(Part part, string input)
        {
            int degrees;
            if (!TryParseHueInput(input, out degrees))
            {
                ModLog.Feedback("[LuxTint] Hue must be H0\u2013H359 (e.g. H193).");
                return false;
            }

            SetHueDegrees(part, degrees);
            _rainbowAll = false;
            int i = (int)part;
            if (!_enabled[i])
            {
                _enabled[i] = true;
                CaptureItemBeforeLux(part);
                EnablePart(part);
            }
            else
            {
                ApplyGlow(part);
                ScheduleRefresh(part);
            }

            ModLog.Feedback("[LuxTint] " + PartLabel(part) + " -> H:" + degrees);
            return true;
        }

        public static void ApplyPickerHue(Part part, int degrees)
        {
            ApplyPickerSelection(part, degrees / 360f,
                GetSaturation(part), GetBrightnessLuminance01(part));
        }

        public static bool RainbowAllEnabled => _rainbowAll;

        public static int GetRainbowBrightnessPercent()
        {
            return GetBrightnessPercent(Part.Bike);
        }

        public static int GetRainbowSpeedPercent()
        {
            return _rainbowSpeedLevel * BrightnessPercentStep;
        }

        private static float GetRainbowHueSpeed()
        {
            return RainbowHueSpeedPerLevel * _rainbowSpeedLevel;
        }

        public static void ToggleRainbowAll()
        {
            if (_rainbowAll)
                DisableRainbowAll();
            else
                EnableRainbowAll();
        }

        public static void EnableRainbowAll()
        {
            _rainbowAll = true;
            _rainbowHue = UnityEngine.Random.value;
            // Shared brightness across all slots for one control.
            int bright = _brightness[(int)Part.Bike];
            for (int p = 0; p < AllParts.Length; p++)
            {
                Part part = AllParts[p];
                int i = (int)part;
                _brightness[i] = bright;
                _saturation[i] = 1f;
                _hue[i] = _rainbowHue;
                if (!_enabled[i])
                {
                    _enabled[i] = true;
                    CaptureItemBeforeLux(part);
                    EnablePart(part);
                }
                else
                {
                    ApplyGlow(part);
                    ScheduleRefresh(part);
                }
            }
            _forceRebuildUntil = Time.unscaledTime + 8f;
            ModLog.Feedback("[LuxTint] Everything Rainbow ON @ "
                + GetRainbowBrightnessPercent() + "% / spd " + GetRainbowSpeedPercent() + "%");
        }

        public static void DisableRainbowAll()
        {
            if (!_rainbowAll && !AnyEnabled) return;
            _rainbowAll = false;
            DisableAll();
        }

        public static void StepRainbowBrightness(int delta)
        {
            int next = _brightness[(int)Part.Bike] + delta;
            if (next < MinBrightnessLevel) next = MinBrightnessLevel;
            if (next > MaxBrightnessLevel) next = MaxBrightnessLevel;
            for (int p = 0; p < AllParts.Length; p++)
                _brightness[(int)AllParts[p]] = next;

            if (_rainbowAll || AnyEnabled)
            {
                for (int p = 0; p < AllParts.Length; p++)
                {
                    Part part = AllParts[p];
                    if (!IsPartEnabled(part)) continue;
                    ApplyGlow(part);
                }
            }

            ModLog.Feedback("[LuxTint] Rainbow brightness -> " + GetRainbowBrightnessPercent() + "%");
        }

        public static void StepRainbowSpeed(int delta)
        {
            int next = _rainbowSpeedLevel + delta;
            if (next < MinRainbowSpeedLevel) next = MinRainbowSpeedLevel;
            if (next > MaxRainbowSpeedLevel) next = MaxRainbowSpeedLevel;
            _rainbowSpeedLevel = next;
            ModLog.Feedback("[LuxTint] Rainbow speed -> " + GetRainbowSpeedPercent() + "%");
        }

        public static bool IsPartEnabled(Part part) => _enabled[(int)part];

        public static string GetPartDisplayName(Part part)
        {
            if (!IsPartEnabled(part)) return "Vanilla";
            if (_rainbowAll) return "RAINBOW " + GetBrightnessPercent(part) + "%";
            return "H:" + GetHueDegrees(part) + " " + GetBrightnessPercent(part) + "%";
        }

        public static int GetHueDegrees(Part part)
        {
            float h = _hue[(int)part];
            int d = (int)(h * 360f);
            if (d < 0) d = 0;
            if (d > 359) d = 359;
            return d;
        }

        public static void SetHueDegrees(Part part, int degrees)
        {
            int i = (int)part;
            degrees = ((degrees % 360) + 360) % 360;
            _hue[i] = degrees / 360f;
        }

        public static string ExportPresetString()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder(80);
            sb.Append("R:");
            sb.Append(_rainbowAll ? '1' : '0');
            sb.Append(':');
            sb.Append(_brightness[(int)Part.Bike]);
            sb.Append(':');
            sb.Append(_rainbowSpeedLevel);
            for (int p = 0; p < AllParts.Length; p++)
            {
                Part part = AllParts[p];
                int slot = (int)part;
                sb.Append('|');
                sb.Append(slot);
                sb.Append(':');
                sb.Append(_enabled[slot] ? '1' : '0');
                sb.Append(':');
                sb.Append(GetHueDegrees(part));
                sb.Append(':');
                sb.Append(_brightness[slot]);
                sb.Append(':');
                sb.Append(GetSaturationPercent(part));
            }
            return sb.ToString();
        }

        public static bool ImportPresetString(string data)
        {
            if (string.IsNullOrEmpty(data)) return false;

            string[] chunks = data.Split('|');
            if (chunks.Length == 0) return false;

            bool wantRainbow = false;
            int rainbowBright = DefaultBrightnessLevel;
            int rainbowSpeed = DefaultRainbowSpeedLevel;
            int startChunk = 0;
            if (chunks.Length > 0 && chunks[0].Length > 0 && (chunks[0][0] == 'R' || chunks[0][0] == 'r'))
            {
                string[] rf = chunks[0].Split(':');
                if (rf.Length >= 2)
                    wantRainbow = rf[1] == "1";
                if (rf.Length >= 3)
                {
                    int b;
                    if (int.TryParse(rf[2], out b))
                        rainbowBright = Mathf.Clamp(b, MinBrightnessLevel, MaxBrightnessLevel);
                }
                if (rf.Length >= 4)
                {
                    int s;
                    if (int.TryParse(rf[3], out s))
                        rainbowSpeed = Mathf.Clamp(s, MinRainbowSpeedLevel, MaxRainbowSpeedLevel);
                }
                startChunk = 1;
            }

            bool[] wantEnabled = new bool[15];
            int[] wantHue = new int[15];
            int[] wantBright = new int[15];
            float[] wantSat = new float[15];
            for (int i = 0; i < 15; i++)
            {
                wantEnabled[i] = false;
                wantHue[i] = GetHueDegrees(Part.Bike);
                wantBright[i] = DefaultBrightnessLevel;
                wantSat[i] = 1f;
            }

            int parsed = 0;
            for (int c = startChunk; c < chunks.Length; c++)
            {
                string[] fields = chunks[c].Split(':');
                if (fields.Length < 3) continue;
                int slot;
                if (!int.TryParse(fields[0], out slot)) continue;
                if (slot < 0 || slot >= 15) continue;
                wantEnabled[slot] = fields[1] == "1";
                int hue;
                if (!int.TryParse(fields[2], out hue)) hue = 0;
                wantHue[slot] = hue;
                if (fields.Length >= 4)
                {
                    int bright;
                    if (int.TryParse(fields[3], out bright))
                        wantBright[slot] = Mathf.Clamp(bright, MinBrightnessLevel, MaxBrightnessLevel);
                }
                if (fields.Length >= 5)
                {
                    int satPct;
                    if (int.TryParse(fields[4], out satPct))
                        wantSat[slot] = Mathf.Clamp01(satPct / 100f);
                }
                parsed++;
            }

            if (parsed == 0 && !wantRainbow) return false;

            if (wantRainbow)
            {
                _rainbowSpeedLevel = rainbowSpeed;
                for (int p = 0; p < AllParts.Length; p++)
                    wantBright[(int)AllParts[p]] = rainbowBright;
                for (int p = 0; p < AllParts.Length; p++)
                {
                    Part part = AllParts[p];
                    int slot = (int)part;
                    SetHueDegrees(part, wantHue[slot]);
                    _brightness[slot] = wantBright[slot];
                    _saturation[slot] = 1f;
                }
                EnableRainbowAll();
                return true;
            }

            _rainbowAll = false;
            for (int p = 0; p < AllParts.Length; p++)
            {
                Part part = AllParts[p];
                int slot = (int)part;
                SetHueDegrees(part, wantHue[slot]);
                _brightness[slot] = wantBright[slot];
                _saturation[slot] = wantSat[slot];
                if (wantEnabled[slot] && !_enabled[slot])
                {
                    _enabled[slot] = true;
                    CaptureItemBeforeLux(part);
                    EnablePart(part);
                }
                else if (!wantEnabled[slot] && _enabled[slot])
                {
                    _enabled[slot] = false;
                    DisablePartInternal(part);
                }
                else if (wantEnabled[slot])
                {
                    ApplyGlow(part);
                    ScheduleRefresh(part);
                }
            }

            return true;
        }

        public static bool AnyEnabled
        {
            get
            {
                return _enabled[(int)Part.Bike]
                    || _enabled[(int)Part.Head]
                    || _enabled[(int)Part.Torso]
                    || _enabled[(int)Part.Legs]
                    || _enabled[(int)Part.Eyes];
            }
        }

        public static void TogglePart(Part part)
        {
            _rainbowAll = false;
            int i = (int)part;
            _enabled[i] = !_enabled[i];
            if (_enabled[i])
            {
                CaptureItemBeforeLux(part);
                EnablePart(part);
                ModLog.Feedback("[LuxTint] " + PartLabel(part) + " -> H:" + GetHueDegrees(part));
            }
            else
            {
                DisablePartInternal(part);
                ModLog.Feedback("[LuxTint] " + PartLabel(part) + " -> vanilla");
            }
        }

        public static void NextPreset(Part part)
        {
            StepHue(part, HueStepDegrees);
        }

        public static void PrevPreset(Part part)
        {
            StepHue(part, -HueStepDegrees);
        }

        private static void StepHue(Part part, int deltaDegrees)
        {
            _rainbowAll = false;
            int i = (int)part;
            SetHueDegrees(part, GetHueDegrees(part) + deltaDegrees);
            if (!_enabled[i])
            {
                _enabled[i] = true;
                CaptureItemBeforeLux(part);
                EnablePart(part);
            }
            else
            {
                ApplyGlow(part);
                ScheduleRefresh(part);
            }
            ModLog.Feedback("[LuxTint] " + PartLabel(part) + " -> H:" + GetHueDegrees(part));
        }

        public static void DisablePart(Part part)
        {
            _rainbowAll = false;
            _enabled[(int)part] = false;
            DisablePartInternal(part);
            ModLog.Feedback("[LuxTint] " + PartLabel(part) + " -> vanilla");
        }

        public static void DisableAll()
        {
            ClearAllEnabledFlags();
            MelonCoroutines.Start(DisableAllRoutine());
            ModLog.Feedback("[LuxTint] all -> vanilla");
        }

        /// <summary>
        /// Quit must not unequip/equip — sync unequip left empty slots and SaveOutfit
        /// persisted a hands/feet-only rider. Mark a pending restore; boot finishes it.
        /// </summary>
        public static void OnApplicationQuit()
        {
            if (!_rainbowAll && !AnyEnabled) return;
            try
            {
                ClearAllEnabledFlags();
                if ((object)_pendingVanillaEntry != null)
                {
                    _pendingVanillaEntry.Value = true;
                    MelonPreferences.Save();
                }
                ModLog.Debug("[LuxTint] Quit: pending vanilla restore on next boot");
            }
            catch (Exception ex)
            {
                ModLog.Warn("[LuxTint] Quit: " + ex.Message);
            }
        }

        private static void ClearAllEnabledFlags()
        {
            _rainbowAll = false;
            _enabled[(int)Part.Bike] = false;
            _enabled[(int)Part.Head] = false;
            _enabled[(int)Part.Torso] = false;
            _enabled[(int)Part.Legs] = false;
            _enabled[(int)Part.Eyes] = false;
            for (int p = 0; p < AllParts.Length; p++)
            {
                int i = (int)AllParts[p];
                _brightness[i] = DefaultBrightnessLevel;
                _saturation[i] = 1f;
            }
            for (int g = 0; g < _equipGen.Length; g++)
                _equipGen[g]++;
        }

        private static IEnumerator DisableAllRoutine()
        {
            _restoredKnownItem = false;
            // Head before Eyes — shared head bone; never leave helmet empty.
            Part[] order = { Part.Bike, Part.Head, Part.Torso, Part.Legs, Part.Eyes };
            for (int i = 0; i < order.Length; i++)
            {
                Part part = order[i];
                ReleasePartVisualState(part);
                RestoreSavedItem(part);
                yield return RestorePartRoutine(part);
            }

            PlayerCustomization pc = GetLocalCustomization();
            ForceStockHueAnimateOnPlayer(pc);
            // One more pass so HueAnimate can overwrite any leftover HDR from our tint.
            if ((object)pc != null)
            {
                for (int i = 0; i < order.Length; i++)
                    ReleaseGlowOnSlotRoots(pc, order[i]);
                ForceStockHueAnimateOnPlayer(pc);
            }

            // Only rewrite the game outfit if we put back known previous pieces.
            if (_restoredKnownItem)
                PersistOutfitAfterVanilla();
        }

        private static void PersistOutfitAfterVanilla()
        {
            try
            {
                PlayerCustomization pc = GetLocalCustomization();
                if ((object)pc == null) return;
                MethodInfo save = typeof(PlayerCustomization).GetMethod("SaveOutfit", Flags);
                if ((object)save != null)
                    save.Invoke(pc, null);
            }
            catch (Exception ex)
            {
                ModLog.Warn("[LuxTint] SaveOutfit after vanilla: " + ex.Message);
            }
        }

        public static bool IsHueAnimateFrozen(HueAnimate ha)
        {
            if ((object)ha == null) return false;
            return _frozenHueIds.Contains(ha.GetInstanceID());
        }

        public static void OnSceneUnloaded()
        {
            for (int g = 0; g < _equipGen.Length; g++)
                _equipGen[g]++;
            _haOriginalSpeed.Clear();
            _frozenHueIds.Clear();
            _originalSharedMats.Clear();
            for (int i = 0; i < _pendingRestoreId.Length; i++)
                _pendingRestoreId[i] = NoSavedItem;
            for (int i = 0; i < _activeByPart.Length; i++)
                if (_activeByPart[i] != null)
                    _activeByPart[i].Clear();
            _pcCache = null;
            // Keep forcing rebuilds after the next map finishes loading.
            _forceRebuildUntil = 0f;
            _bootRepairGen++;
        }

        /// <summary>
        /// Lux stays logically ON across maps, but freeze/targets are cleared on unload.
        /// Force Tick to rebuild glow for a while after the new rider spawns.
        /// Also finishes quit-time vanilla restore and repairs stripped outfits.
        /// </summary>
        public static void OnSceneInitialized()
        {
            int gen = ++_bootRepairGen;
            MelonCoroutines.Start(BootRepairRoutine(gen));

            if (!AnyEnabled) return;
            _forceRebuildUntil = Time.unscaledTime + 25f;
            MelonCoroutines.Start(ReapplyAfterMapChangeRoutine());
        }

        private static IEnumerator BootRepairRoutine(int gen)
        {
            PlayerCustomization pc = null;
            for (int w = 0; w < 300; w++)
            {
                if (gen != _bootRepairGen) yield break;
                pc = GetLocalCustomization();
                if ((object)pc != null && ResolveRefs())
                    break;
                yield return null;
            }

            if ((object)pc == null || gen != _bootRepairGen) yield break;

            bool pending = (object)_pendingVanillaEntry != null && _pendingVanillaEntry.Value;
            if (pending)
            {
                _pendingVanillaEntry.Value = false;
                MelonPreferences.Save();
            }

            // Wait past StatsManager DeferredEnsureSavedBike (120 + 60 frames of SetBike),
            // which rebuilds the rider and was wiping any early outfit repair.
            for (int w = 0; w < 220; w++)
            {
                if (gen != _bootRepairGen) yield break;
                yield return null;
            }

            if (pending && HasAnyCapturedPreLux())
            {
                ModLog.Feedback("[LuxTint] Restoring pre-Lux gear after quit...");
                yield return DisableAllRoutine();
            }

            if (gen != _bootRepairGen) yield break;

            // Always try ActiveSlot / first preset when body/bike look stripped.
            // Detection uses missing item OR missing spawned prefab (hands/feet-only).
            for (int pass = 0; pass < 3; pass++)
            {
                if (gen != _bootRepairGen) yield break;
                pc = GetLocalCustomization();
                if ((object)pc == null) yield break;
                if (!IsCriticalGearMissing(pc)) yield break;

                ModLog.Feedback("[LuxTint] Missing body/bike — repairing outfit (pass "
                    + (pass + 1) + ")...");
                if (HasAnyCapturedPreLux())
                    yield return DisableAllRoutine();

                pc = GetLocalCustomization();
                if ((object)pc != null && !IsCriticalGearMissing(pc))
                    yield break;

                OutfitPresets.ForceReapplyActiveOutfit();
                for (int w = 0; w < 90; w++)
                {
                    if (gen != _bootRepairGen) yield break;
                    yield return null;
                }
            }

            pc = GetLocalCustomization();
            if ((object)pc != null && IsCriticalGearMissing(pc))
                ModLog.Warn("[LuxTint] Outfit still missing — open Outfit and press LOAD on a preset.");
        }

        public static bool NeedsOutfitRepair()
        {
            PlayerCustomization pc = GetLocalCustomization();
            return (object)pc != null && IsCriticalGearMissing(pc);
        }

        private static bool HasAnyCapturedPreLux()
        {
            for (int p = 0; p < AllParts.Length; p++)
            {
                int i = (int)AllParts[p];
                if (_savedItemId[i] != NoSavedItem) return true;
                if ((object)_savedEntries[i] != null && _savedEntries[i].Value != NoSavedItem)
                    return true;
            }
            return false;
        }

        private static bool IsCriticalGearMissing(PlayerCustomization pc)
        {
            if ((object)pc == null || !ResolveRefs()) return false;
            // Hands/feet alone = torso/legs/bike missing or prefab never spawned.
            return IsSlotBroken(pc, Part.Torso)
                || IsSlotBroken(pc, Part.Legs)
                || IsSlotBroken(pc, Part.Bike);
        }

        private static bool IsSlotBroken(PlayerCustomization pc, Part part)
        {
            try
            {
                object slotVal = Enum.ToObject(_slotEnumType, (int)part);
                object inst = _getItemInstance.Invoke(pc, new object[] { slotVal });
                if ((object)inst == null) return true;
                CustomizationItem cur = _itemInstanceItem.GetValue(inst) as CustomizationItem;
                if ((object)cur == null) return true;
                GameObject go = _itemInstancePrefab.GetValue(inst) as GameObject;
                return !UnityNull.Alive(go);
            }
            catch
            {
                return false;
            }
        }

        private static IEnumerator ReapplyAfterMapChangeRoutine()
        {
            float until = _forceRebuildUntil;
            for (int i = 0; i < 400; i++)
            {
                if (!AnyEnabled) yield break;
                if (Time.unscaledTime > until + 5f) yield break;

                PlayerCustomization pc = GetLocalCustomization();
                if ((object)pc != null)
                {
                    for (int p = 0; p < AllParts.Length; p++)
                    {
                        Part part = AllParts[p];
                        if (!IsPartEnabled(part)) continue;
                        ApplySlotGlow(part);
                    }
                }
                yield return null;
            }
        }

        public static void Tick()
        {
            if (!AnyEnabled) return;
            _luxTick++;

            if (_rainbowAll)
            {
                _rainbowHue = Mathf.Repeat(_rainbowHue + GetRainbowHueSpeed() * Time.deltaTime, 1f);
                for (int p = 0; p < AllParts.Length; p++)
                    _hue[(int)AllParts[p]] = _rainbowHue;
            }

            PlayerCustomization pcNow = GetLocalCustomization();
            if ((object)pcNow != null)
            {
                int pcId = pcNow.GetInstanceID();
                if (pcId != _lastPcInstanceId)
                {
                    // New rider instance (map/seed hop) — rebuild even if scene callbacks were odd.
                    _lastPcInstanceId = pcId;
                    _frozenHueIds.Clear();
                    _originalSharedMats.Clear();
                    for (int i = 0; i < _activeByPart.Length; i++)
                        if (_activeByPart[i] != null)
                            _activeByPart[i].Clear();
                    _forceRebuildUntil = Time.unscaledTime + 25f;
                    ModLog.Debug("[LuxTint] New PlayerCustomization — forcing lux rebuild.");
                }
            }
            else
            {
                _lastPcInstanceId = 0;
            }

            bool forceRebuild = Time.unscaledTime < _forceRebuildUntil;
            // Rainbow needs every-frame paint so the cycle is smooth; also after map change.
            if (forceRebuild || _rainbowAll)
            {
                if (_enabled[(int)Part.Bike]) ApplySlotGlow(Part.Bike);
                if (_enabled[(int)Part.Head]) ApplySlotGlow(Part.Head);
                if (_enabled[(int)Part.Torso]) ApplySlotGlow(Part.Torso);
                if (_enabled[(int)Part.Legs]) ApplySlotGlow(Part.Legs);
                if (_enabled[(int)Part.Eyes]) ApplySlotGlow(Part.Eyes);
                return;
            }

            bool rebuild = (_luxTick % 30) == 1;
            if (!rebuild && (_luxTick % 5) != 0)
                return;
            if (rebuild)
            {
                if (_enabled[(int)Part.Bike]) ApplySlotGlow(Part.Bike);
                if (_enabled[(int)Part.Head]) ApplySlotGlow(Part.Head);
                if (_enabled[(int)Part.Torso]) ApplySlotGlow(Part.Torso);
                if (_enabled[(int)Part.Legs]) ApplySlotGlow(Part.Legs);
                if (_enabled[(int)Part.Eyes]) ApplySlotGlow(Part.Eyes);
            }
            else
            {
                if (_enabled[(int)Part.Bike]) PaintCachedGlow(Part.Bike);
                if (_enabled[(int)Part.Head]) PaintCachedGlow(Part.Head);
                if (_enabled[(int)Part.Torso]) PaintCachedGlow(Part.Torso);
                if (_enabled[(int)Part.Legs]) PaintCachedGlow(Part.Legs);
                if (_enabled[(int)Part.Eyes]) PaintCachedGlow(Part.Eyes);
            }
        }

        private static void ApplySlotGlow(Part part)
        {
            PlayerCustomization pc = GetLocalCustomization();
            if ((object)pc == null) return;

            // Do not gate on IsLuxItemEquipped / rarity — after map change the glow
            // meshes are present before rarity reads settle. CollectGlowTargets is enough.

            int pi = (int)part;
            if (_activeByPart[pi] == null)
                _activeByPart[pi] = new List<GlowTarget>(8);
            else
                _activeByPart[pi].Clear();

            CollectGlowTargets(pc, part, _scratchTargets);
            if (_scratchTargets.Count == 0)
                return;

            float hue = _hue[pi];
            for (int i = 0; i < _scratchTargets.Count; i++)
            {
                GlowTarget t = _scratchTargets[i];
                if (!UnityNull.Alive(t.Renderer)) continue;
                ApplyGlowToRenderer(t.Renderer, t.Hue, hue, part);
                _activeByPart[pi].Add(t);
            }

            FreezeHueAnimatesForSlot(pc, part, hue);

            // Once every enabled part has targets, we can stop the post-map spam.
            if (Time.unscaledTime < _forceRebuildUntil && AllEnabledPartsHaveTargets())
                _forceRebuildUntil = Time.unscaledTime + 1.5f;
        }

        private static bool AllEnabledPartsHaveTargets()
        {
            for (int p = 0; p < AllParts.Length; p++)
            {
                Part part = AllParts[p];
                if (!IsPartEnabled(part)) continue;
                List<GlowTarget> list = _activeByPart[(int)part];
                if (list == null || list.Count == 0)
                    return false;
            }
            return true;
        }

        private static void FreezeHueAnimatesForSlot(PlayerCustomization pc, Part part, float hue)
        {
            GameObject prefab = GetSlotSpawnedPrefab(pc, part);
            if (UnityNull.Alive(prefab))
                FreezeHueAnimatesUnder(prefab, hue, part);

            if (part == Part.Bike)
            {
                Transform bikeModel = FindChildRecursive(pc.transform, "BikeModel");
                if (UnityNull.Alive(bikeModel))
                    FreezeHueAnimatesUnder(bikeModel.gameObject, hue, part);
            }
        }

        private static void FreezeHueAnimatesUnder(GameObject root, float hue, Part part)
        {
            HueAnimate[] hues = root.GetComponentsInChildren<HueAnimate>(true);
            if ((object)hues == null) return;
            for (int i = 0; i < hues.Length; i++)
            {
                HueAnimate ha = hues[i];
                if ((object)ha == null) continue;
                FreezeHueAnimate(ha, hue, part);
            }
        }

        private static void PaintCachedGlow(Part part)
        {
            int pi = (int)part;
            List<GlowTarget> list = _activeByPart[pi];
            if (list == null || list.Count == 0)
            {
                ApplySlotGlow(part);
                return;
            }
            float hue = _hue[pi];
            bool anyAlive = false;
            for (int i = 0; i < list.Count; i++)
            {
                GlowTarget t = list[i];
                if (!UnityNull.Alive(t.Renderer)) continue;
                anyAlive = true;
                ApplyGlowToRenderer(t.Renderer, t.Hue, hue, part);
            }
            if (!anyAlive)
            {
                ApplySlotGlow(part);
                return;
            }
            PlayerCustomization pc = GetLocalCustomization();
            if ((object)pc != null)
                FreezeHueAnimatesForSlot(pc, part, hue);
        }

        private static void ReapplyCachedGlow(Part part)
        {
            PaintCachedGlow(part);
        }

        private static void EnablePart(Part part)
        {
            EnsureGlowItemEquipped(part);
            ScheduleRefresh(part);
        }

        private static void DisablePartInternal(Part part)
        {
            _equipGen[(int)part]++;
            ReleasePartVisualState(part);
            RestoreSavedItem(part);
            ScheduleRestore(part);
        }

        private static void ReleasePartVisualState(Part part)
        {
            int pi = (int)part;
            ClearGlowTargets(_activeByPart[pi]);
            if (_activeByPart[pi] != null)
                _activeByPart[pi].Clear();

            PlayerCustomization pc = GetLocalCustomization();
            if ((object)pc == null) return;

            ReleaseGlowOnSlotRoots(pc, part);
        }

        private static void ReleaseGlowOnSlotRoots(PlayerCustomization pc, Part part)
        {
            if (!ResolveRefs()) return;
            try
            {
                ReleaseGlowOnEquippedPrefab(pc, part);

                if (part == Part.Bike)
                {
                    Transform bikeModel = FindChildRecursive(pc.transform, "BikeModel");
                    if (UnityNull.Alive(bikeModel))
                    {
                        UnfreezeAllHueAnimatesUnder(bikeModel.gameObject);
                        ResetAllRendererMaterialsUnder(bikeModel.gameObject);
                    }
                    if ((object)_refreshBikeMesh != null)
                        _refreshBikeMesh.Invoke(pc, null);
                }
            }
            catch (Exception ex)
            {
                ModLog.Warn("[LuxTint] ReleaseGlowOnSlotRoots: " + ex.Message);
            }
        }

        private static void ReleaseGlowOnEquippedPrefab(PlayerCustomization pc, Part part)
        {
            object slotVal = Enum.ToObject(_slotEnumType, (int)part);
            object inst = _getItemInstance.Invoke(pc, new object[] { slotVal });
            if ((object)inst != null)
            {
                GameObject go = _itemInstancePrefab.GetValue(inst) as GameObject;
                if (UnityNull.Alive(go))
                {
                    UnfreezeAllHueAnimatesUnder(go);
                    ResetAllRendererMaterialsUnder(go);
                }
            }
        }

        private static void CaptureItemBeforeLux(Part part)
        {
            if (!ResolveRefs()) return;
            int i = (int)part;

            PlayerCustomization pc = GetLocalCustomization();
            if ((object)pc == null) return;

            object slotVal = Enum.ToObject(_slotEnumType, i);
            object inst = _getItemInstance.Invoke(pc, new object[] { slotVal });
            if ((object)inst == null)
            {
                _savedItemId[i] = NoSavedItem;
                return;
            }

            CustomizationItem cur = _itemInstanceItem.GetValue(inst) as CustomizationItem;
            if ((object)cur == null)
            {
                _savedItemId[i] = NoSavedItem;
                return;
            }

            // Drop bad captures: previous id must not itself be a Lux glow piece.
            if (_savedItemId[i] != NoSavedItem)
            {
                CustomizationManager cmCheck = UnityEngine.Object.FindObjectOfType<CustomizationManager>();
                if ((object)cmCheck != null)
                {
                    CustomizationItem prev = cmCheck.GetItemFromID(_savedItemId[i]);
                    if ((object)prev != null && IsEquippedLuxGlowItem(prev, part))
                    {
                        _savedItemId[i] = NoSavedItem;
                        PersistSavedId(part);
                    }
                }
            }

            if (IsEquippedLuxGlowItem(cur, part))
            {
                // Already on Lux — keep the outfit we captured earlier (memory or disk).
                // Never invent stock/standard clothes here.
                if (_savedItemId[i] == NoSavedItem
                    && (object)_savedEntries[i] != null
                    && _savedEntries[i].Value != NoSavedItem)
                {
                    CustomizationManager cmDisk = UnityEngine.Object.FindObjectOfType<CustomizationManager>();
                    int diskId = _savedEntries[i].Value;
                    CustomizationItem diskItem = (object)cmDisk != null
                        ? cmDisk.GetItemFromID(diskId) as CustomizationItem : null;
                    if ((object)diskItem != null && !IsEquippedLuxGlowItem(diskItem, part))
                        _savedItemId[i] = diskId;
                }
                ModLog.Debug("[LuxTint] Already on Lux " + PartLabel(part)
                    + " — previous id " + _savedItemId[i]);
                return;
            }

            // Real pre-Lux gear — this is what All Vanilla must restore.
            _savedItemId[i] = cur.itemID;
            PersistSavedId(part);
            ModLog.Debug("[LuxTint] Captured pre-Lux " + PartLabel(part)
                + " id " + _savedItemId[i] + " (" + cur.displayName + ")");
        }

        private static void ApplyGlow(Part part)
        {
            ApplySlotGlow(part);
        }

        private static void ApplyGlowToRenderer(Renderer rend, HueAnimate ha, float hue, Part part)
        {
            if (!UnityNull.Alive(rend)) return;
            CacheOriginalSharedMaterials(rend);

            if ((object)ha != null)
            {
                Renderer driveRend = ha.GetComponent<Renderer>();
                if (!UnityNull.Alive(driveRend))
                    driveRend = rend;
                if (!UnityNull.Alive(driveRend)) return;
                CacheOriginalSharedMaterials(driveRend);
                DriveLuxMaterials(driveRend, ha, hue, part);
                FreezeHueAnimate(ha, hue, part);
                return;
            }

            ResolveHueAnimateFields();
            Material[] mats = rend.materials;
            LagDiag.LuxMaterialsAccess++;
            if ((object)mats == null) return;

            float hdrV = GetHdrV(part);
            int start = MaterialStartIndex(part, mats.Length);
            for (int m = start; m < mats.Length; m++)
            {
                Material mat = mats[m];
                if ((object)mat == null) continue;
                bool emissionActive = mat.IsKeywordEnabled("_EMISSION");
                if (!emissionActive && mat.HasProperty("_EmissionColor"))
                {
                    Color e = mat.GetColor("_EmissionColor");
                    emissionActive = e.maxColorComponent > 0.01f;
                }
                if (!emissionActive) continue;

                Color glow = Color.HSVToRGB(hue, _saturation[(int)part], hdrV, hdr: true);
                if (mat.HasProperty("_EmissionColor"))
                    mat.SetColor("_EmissionColor", glow);
                if (mat.HasProperty("_Color"))
                    mat.SetColor("_Color", glow);
            }
        }

        private static void DriveLuxMaterials(Renderer rend, HueAnimate ha, float hue, Part part)
        {
            if (!UnityNull.Alive(rend) || (object)ha == null) return;
            CacheOriginalSharedMaterials(rend);

            ResolveHueAnimateFields();
            Material[] mats = rend.materials;
            LagDiag.LuxMaterialsAccess++;
            if ((object)mats == null || mats.Length == 0) return;

            float hdrV = GetHdrV(part);
            float sat = _saturation[(int)part];
            float phase = 0f;
            float phaseStep = 0f;
            if (part != Part.Bike && (object)_haPhaseOffset != null)
                phaseStep = (float)_haPhaseOffset.GetValue(ha);

            int start = MaterialStartIndex(part, mats.Length);
            for (int i = start; i < mats.Length; i++)
            {
                Material mat = mats[i];
                if ((object)mat == null) continue;

                Color glow = Color.HSVToRGB(Mathf.Repeat(hue + phase, 1f), sat, hdrV, hdr: true);
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.SetColor("_EmissionColor", glow);
                    if (!mat.IsKeywordEnabled("_EMISSION"))
                        mat.EnableKeyword("_EMISSION");
                }
                if (mat.HasProperty("_Color"))
                    mat.SetColor("_Color", glow);
                phase += phaseStep;
            }
        }

        private static int MaterialStartIndex(Part part, int matCount)
        {
            if (part == Part.Bike || part == Part.Eyes)
                return 0;
            if (matCount > 1)
                return 1;
            return 0;
        }

        private static bool ShouldApplyGlowNow(PlayerCustomization pc, Part part)
        {
            if (part == Part.Bike)
                return true;
            if (part == Part.Head || part == Part.Eyes)
                return IsLuxItemEquipped(pc, part);
            if (part == Part.Torso || part == Part.Legs)
                return IsLuxGlowVisualReady(pc, part);
            return true;
        }

        private static bool IsLuxGlowVisualReady(PlayerCustomization pc, Part part)
        {
            if (!IsLuxItemEquipped(pc, part))
                return false;

            GameObject prefab = GetSlotSpawnedPrefab(pc, part);
            if (!UnityNull.Alive(prefab))
                return false;

            if (prefab.GetComponentsInChildren<HueAnimate>(true).Length > 0)
                return true;

            Transform linkRoot = GetLinkSpawnParent(pc, part);
            return UnityNull.Alive(linkRoot)
                && linkRoot.GetComponentsInChildren<HueAnimate>(true).Length > 0;
        }

        private static bool IsLuxItemEquipped(PlayerCustomization pc, Part part)
        {
            if (!ResolveRefs() || (object)pc == null) return false;
            try
            {
                object slotVal = Enum.ToObject(_slotEnumType, (int)part);
                object inst = _getItemInstance.Invoke(pc, new object[] { slotVal });
                if ((object)inst == null) return false;
                CustomizationItem cur = _itemInstanceItem.GetValue(inst) as CustomizationItem;
                return IsExtraordinary(cur);
            }
            catch
            {
                return false;
            }
        }

        private static bool ShouldSkipApparelBaseMaterial(Part part, int matIndex, int matCount)
        {
            return part != Part.Bike && part != Part.Eyes && matCount > 1 && matIndex == 0;
        }

        private static Transform GetLinkSpawnParent(PlayerCustomization pc, Part part)
        {
            if (!ResolveRefs() || (object)pc == null || (object)_getLinkForSlot == null)
                return null;
            try
            {
                object slotVal = Enum.ToObject(_slotEnumType, (int)part);
                object link = _getLinkForSlot.Invoke(pc, new object[] { slotVal });
                if ((object)link == null) return null;
                if ((object)_linkSpawnParent != null)
                    return _linkSpawnParent.GetValue(link) as Transform;
                return ((CustomizationLink)link).spawnParent;
            }
            catch
            {
                return null;
            }
        }

        private static void FreezeHueAnimate(HueAnimate ha, float hue, Part part)
        {
            if ((object)ha == null) return;
            // Do NOT zero HueAnimate speed — Harmony Prefix blocks Update while frozen.
            // Zeroing speed caused All Vanilla to leave stock Lux stuck on one blown-out colour.
            ResolveHueAnimateFields();
            if ((object)_haHueAccum != null)
                _haHueAccum.SetValue(ha, hue);
            _frozenHueIds.Add(ha.GetInstanceID());
            ha.enabled = true;
        }

        private static void RestoreHueAnimate(HueAnimate ha)
        {
            if ((object)ha == null) return;
            _frozenHueIds.Remove(ha.GetInstanceID());
            ResolveHueAnimateFields();
            // Repair any older builds that zeroed speed while frozen.
            float spd;
            if (_haOriginalSpeed.TryGetValue(ha, out spd) && (object)_haSpeed != null)
            {
                if (spd > 0.0001f)
                    _haSpeed.SetValue(ha, spd);
                else
                    _haSpeed.SetValue(ha, 1f);
            }
            else if ((object)_haSpeed != null)
            {
                float cur = (float)_haSpeed.GetValue(ha);
                if (cur <= 0.0001f)
                    _haSpeed.SetValue(ha, 1f);
            }
            if ((object)_haHueAccum != null)
                _haHueAccum.SetValue(ha, UnityEngine.Random.value);
            _haOriginalSpeed.Remove(ha);
            ha.enabled = true;
        }

        /// <summary>
        /// Force every HueAnimate under the local rider back to stock rainbow (unfreeze + speed).
        /// </summary>
        private static void ForceStockHueAnimateOnPlayer(PlayerCustomization pc)
        {
            if ((object)pc == null) return;
            ResolveHueAnimateFields();

            // Drop freeze gate first so Update can run even if a component was missed below.
            _frozenHueIds.Clear();

            HueAnimate[] hues = pc.GetComponentsInChildren<HueAnimate>(true);
            if ((object)hues == null) return;
            for (int i = 0; i < hues.Length; i++)
            {
                HueAnimate ha = hues[i];
                if ((object)ha == null) continue;
                float spd;
                if (_haOriginalSpeed.TryGetValue(ha, out spd) && (object)_haSpeed != null && spd > 0.0001f)
                    _haSpeed.SetValue(ha, spd);
                else if ((object)_haSpeed != null)
                {
                    float cur = (float)_haSpeed.GetValue(ha);
                    if (cur <= 0.0001f)
                        _haSpeed.SetValue(ha, 1f);
                }
                if ((object)_haHueAccum != null)
                    _haHueAccum.SetValue(ha, UnityEngine.Random.value);
                ha.enabled = true;
            }
            _haOriginalSpeed.Clear();
        }

        private static void ResolveHueAnimateFields()
        {
            if ((object)_haSpeed != null) return;

            Type t = typeof(HueAnimate);
            FieldInfo[] fields = t.GetFields(Flags);
            List<FieldInfo> pubFloats = new List<FieldInfo>(2);
            FieldInfo privHue = null;

            for (int i = 0; i < fields.Length; i++)
            {
                FieldInfo f = fields[i];
                if (!f.FieldType.Equals(typeof(float))) continue;
                if (f.IsPublic)
                    pubFloats.Add(f);
                else if ((object)privHue == null)
                    privHue = f;
            }

            if (pubFloats.Count > 1)
                pubFloats.Sort((a, b) => a.MetadataToken.CompareTo(b.MetadataToken));
            if (pubFloats.Count >= 1) _haSpeed = pubFloats[0];
            if (pubFloats.Count >= 2) _haPhaseOffset = pubFloats[1];
            _haHueAccum = privHue;
        }

        private static void ClearGlow(Part part)
        {
            int pi = (int)part;
            ClearGlowTargets(_activeByPart[pi]);

            PlayerCustomization pc = GetLocalCustomization();
            if ((object)pc == null) return;

            CollectGlowTargets(pc, part, _scratchTargets);
            for (int i = 0; i < _scratchTargets.Count; i++)
                ClearGlowTarget(_scratchTargets[i]);

            if (_activeByPart[pi] != null)
                _activeByPart[pi].Clear();
        }

        private static void ClearGlowTargets(List<GlowTarget> targets)
        {
            if ((object)targets == null) return;
            for (int i = 0; i < targets.Count; i++)
                ClearGlowTarget(targets[i]);
        }

        private static void ClearGlowTarget(GlowTarget t)
        {
            if (!UnityNull.Alive(t.Renderer)) return;

            ResetRendererMaterials(t.Renderer);
            RestoreHueAnimate(t.Hue);
        }

        private static void CacheOriginalSharedMaterials(Renderer rend)
        {
            if (!UnityNull.Alive(rend)) return;
            int id = rend.GetInstanceID();
            if (_originalSharedMats.ContainsKey(id)) return;
            Material[] shared = rend.sharedMaterials;
            if ((object)shared == null) return;
            Material[] copy = new Material[shared.Length];
            for (int i = 0; i < shared.Length; i++)
                copy[i] = shared[i];
            _originalSharedMats[id] = copy;
        }

        private static void ResetRendererMaterials(Renderer rend)
        {
            if (!UnityNull.Alive(rend)) return;
            rend.SetPropertyBlock(null);

            int id = rend.GetInstanceID();
            Material[] orig;
            if (_originalSharedMats.TryGetValue(id, out orig) && (object)orig != null)
            {
                Material[] instanced = null;
                try { instanced = rend.materials; }
                catch { }

                rend.sharedMaterials = orig;

                if ((object)instanced != null)
                {
                    for (int i = 0; i < instanced.Length; i++)
                    {
                        Material m = instanced[i];
                        if ((object)m == null) continue;
                        bool isOrig = false;
                        for (int o = 0; o < orig.Length; o++)
                        {
                            if ((object)orig[o] == (object)m) { isOrig = true; break; }
                        }
                        if (!isOrig)
                            UnityEngine.Object.Destroy(m);
                    }
                }
                _originalSharedMats.Remove(id);
                return;
            }

            // No original cache — leave materials; ForceStockHueAnimateOnPlayer lets
            // vanilla HueAnimate overwrite emission to stock HDR 2.5 next frame.
        }

        private static void ResetAllRendererMaterialsUnder(GameObject root)
        {
            if (!UnityNull.Alive(root)) return;
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if ((object)renderers == null) return;
            for (int i = 0; i < renderers.Length; i++)
                ResetRendererMaterials(renderers[i]);
        }

        private static void EnsureGlowItemEquipped(Part part)
        {
            if (!ResolveRefs()) return;

            PlayerCustomization pc = GetLocalCustomization();
            if ((object)pc == null) return;

            object slotVal = Enum.ToObject(_slotEnumType, (int)part);
            object inst = _getItemInstance.Invoke(pc, new object[] { slotVal });
            CustomizationItem cur = null;
            if ((object)inst != null)
                cur = _itemInstanceItem.GetValue(inst) as CustomizationItem;

            CustomizationItem glowWant = FindGlowItem(part, cur);
            if ((object)glowWant != null && (object)cur != null
                && cur.itemID == glowWant.itemID && HasGlowRenderer(pc, part))
                return;

            CustomizationItem glowItem = glowWant;
            if ((object)glowItem == null)
            {
                ModLog.Warn("[LuxTint] No glow item for " + PartLabel(part));
                return;
            }

            CustomizationManager cm = UnityEngine.Object.FindObjectOfType<CustomizationManager>();
            if ((object)cm != null)
            {
                try
                {
                    if (!cm.IsItemUnlocked(glowItem))
                        cm.UnlockItem(glowItem);
                }
                catch (Exception ex)
                {
                    ModLog.Warn("[LuxTint] UnlockItem: " + ex.Message);
                }
            }

            if (part == Part.Bike)
            {
                try
                {
                    // Keep Sandbox/game preferred bike — Lux bike cosmetics must not
                    // permanently replace the player's default bike index.
                    int preferredBefore = BikeSwitcher.CurrentBikeIndex;
                    int actualBefore = BikeSwitcher.FindBikeIndex(BikeSwitcher.GetCurrentBikeType());

                    _equipItem.Invoke(pc, new object[] { glowItem, true });
                    if ((object)_refreshBikeMesh != null)
                        _refreshBikeMesh.Invoke(pc, null);

                    int restore = preferredBefore >= 0 ? preferredBefore : actualBefore;
                    if (restore >= 0)
                        BikeSwitcher.SetPreferredBikeIndexOnly(restore);

                    ModLog.Debug("[LuxTint] Equipped glow " + PartLabel(part) + ": " + glowItem.displayName
                        + " (kept preferred bike " + restore + ")");
                }
                catch (Exception ex)
                {
                    ModLog.Warn("[LuxTint] Bike equip: " + ex.Message);
                }
                return;
            }

            int gen = _equipGen[(int)part];
            MelonCoroutines.Start(EquipLuxCoroutine(pc, part, glowItem, gen));
        }

        private static IEnumerator EquipLuxCoroutine(
            PlayerCustomization pc, Part part, CustomizationItem luxItem, int gen)
        {
            if (!IsPartEnabled(part) || _equipGen[(int)part] != gen)
                yield break;

            IEnumerator equipRoutine = null;
            try
            {
                if ((object)_equipItemRoutine != null)
                    equipRoutine = _equipItemRoutine.Invoke(pc, new object[] { luxItem, true }) as IEnumerator;
                else if ((object)_equipItem != null)
                    _equipItem.Invoke(pc, new object[] { luxItem, true });
            }
            catch (Exception ex)
            {
                ModLog.Warn("[LuxTint] Lux equip: " + ex.Message);
            }

            if ((object)equipRoutine != null)
            {
                while (equipRoutine.MoveNext())
                {
                    if (!IsPartEnabled(part) || _equipGen[(int)part] != gen)
                        yield break;
                    yield return equipRoutine.Current;
                }
            }

            object slotVal = Enum.ToObject(_slotEnumType, (int)part);
            for (int w = 0; w < 90; w++)
            {
                if (!IsPartEnabled(part) || _equipGen[(int)part] != gen)
                    yield break;

                object inst = _getItemInstance.Invoke(pc, new object[] { slotVal });
                if ((object)inst != null)
                {
                    CustomizationItem cur = _itemInstanceItem.GetValue(inst) as CustomizationItem;
                    GameObject go = _itemInstancePrefab.GetValue(inst) as GameObject;
                    if ((object)cur != null && cur.itemID == luxItem.itemID
                        && IsExtraordinary(cur) && UnityNull.Alive(go))
                        break;
                }
                yield return null;
            }

            if (IsPartEnabled(part) && _equipGen[(int)part] == gen)
            {
                ApplySlotGlow(part);
                ScheduleRefresh(part);
            }
        }

        private static bool HasGlowRenderer(PlayerCustomization pc, Part part)
        {
            CollectGlowTargets(pc, part, _scratchTargets);
            return _scratchTargets.Count > 0;
        }

        private static CustomizationItem FindGlowItem(Part part, CustomizationItem current)
        {
            CustomizationManager cm = UnityEngine.Object.FindObjectOfType<CustomizationManager>();
            CustomizationItem[] pool = null;
            bool wantLong = (part == Part.Legs || part == Part.Torso)
                && WantsLongGarment(part, current);

            if ((object)cm != null && (object)_getItemsOfSlot != null && (object)_slotEnumType != null)
            {
                try
                {
                    object slotVal = Enum.ToObject(_slotEnumType, (int)part);
                    pool = _getItemsOfSlot.Invoke(cm, new object[] { slotVal }) as CustomizationItem[];
                }
                catch { }
            }

            if ((object)pool == null || pool.Length == 0)
                pool = Resources.FindObjectsOfTypeAll<CustomizationItem>();

            CustomizationItem namedLux = null;
            CustomizationItem namedLuxUnlocked = null;
            CustomizationItem namedLuxAny = null;
            CustomizationItem namedLuxUnlockedAny = null;
            CustomizationItem lengthExtra = null;
            CustomizationItem anyExtra = null;

            for (int i = 0; i < pool.Length; i++)
            {
                CustomizationItem item = pool[i];
                if ((object)item == null) continue;
                if (Convert.ToInt32(item.slot) != (int)part) continue;
                if (Convert.ToInt32(item.rarity) != ExtraordinaryRarity) continue;
                if (part == Part.Torso && !IsTorsoLuxGearItem(item, cm))
                    continue;

                bool lengthOk = part != Part.Legs && part != Part.Torso
                    || GarmentLengthMatches(item, part, wantLong);

                string path = (item.assetPath ?? "").ToLowerInvariant();
                string name = (item.displayName ?? "").ToLowerInvariant();
                bool isNamedLux = path.Contains("lux") || name.Contains("lux");

                if (lengthOk)
                {
                    if ((object)lengthExtra == null)
                        lengthExtra = item;
                    if (isNamedLux)
                    {
                        if ((object)namedLux == null)
                            namedLux = item;
                        if ((object)cm != null)
                        {
                            try
                            {
                                if (cm.IsItemUnlocked(item))
                                    namedLuxUnlocked = item;
                            }
                            catch { }
                        }
                    }
                }

                if ((object)anyExtra == null)
                    anyExtra = item;
                if (isNamedLux)
                {
                    if ((object)namedLuxAny == null)
                        namedLuxAny = item;
                    if ((object)cm != null)
                    {
                        try
                        {
                            if (cm.IsItemUnlocked(item))
                                namedLuxUnlockedAny = item;
                        }
                        catch { }
                    }
                }
            }

            if (part == Part.Torso)
            {
                if ((object)namedLuxUnlocked != null) return namedLuxUnlocked;
                if ((object)namedLux != null) return namedLux;
                if ((object)lengthExtra != null) return lengthExtra;
                if ((object)namedLuxUnlockedAny != null) return namedLuxUnlockedAny;
                if ((object)namedLuxAny != null) return namedLuxAny;
                if ((object)anyExtra != null) return anyExtra;
                return null;
            }

            if ((object)namedLuxUnlocked != null) return namedLuxUnlocked;
            if ((object)namedLux != null) return namedLux;
            if ((object)lengthExtra != null) return lengthExtra;
            if ((object)namedLuxUnlockedAny != null) return namedLuxUnlockedAny;
            if ((object)namedLuxAny != null) return namedLuxAny;
            return anyExtra;
        }

        private static bool IsTorsoLuxGearItem(CustomizationItem item, CustomizationManager cm)
        {
            if ((object)item == null) return false;

            if (item.itemID >= LuxCustomGearMinId && item.itemID <= LuxCustomGearMaxId)
            {
                if ((object)cm != null && (object)_isCustomGear != null)
                {
                    try
                    {
                        int idx = (int)_isCustomGear.Invoke(cm, new object[] { item });
                        return idx >= 0;
                    }
                    catch { }
                }
                return true;
            }

            string path = (item.assetPath ?? "").ToLowerInvariant();
            string name = (item.displayName ?? "").ToLowerInvariant();
            return path.Contains("lux") || name.Contains("lux");
        }

        private static bool WantsLongGarment(Part part, CustomizationItem cur)
        {
            if ((object)cur == null) return false;
            string name = cur.displayName ?? "";
            if (part == Part.Legs)
                return name.Contains("Long Pants");
            if (part == Part.Torso)
                return name.Contains("Jersey Long");
            return false;
        }

        private static bool GarmentLengthMatches(CustomizationItem item, Part part, bool wantLong)
        {
            if (part == Part.Legs)
            {
                bool isLong = (item.displayName ?? "").Contains("Long Pants");
                return wantLong == isLong;
            }
            if (part == Part.Torso)
            {
                bool isLong = (item.displayName ?? "").Contains("Jersey Long");
                return wantLong == isLong;
            }
            return true;
        }

        private static bool IsExtraordinary(CustomizationItem item)
        {
            if ((object)item == null) return false;
            return Convert.ToInt32(item.rarity) == ExtraordinaryRarity;
        }

        private static bool IsEquippedLuxGlowItem(CustomizationItem cur, Part part)
        {
            if ((object)cur == null) return false;
            CustomizationItem glow = FindGlowItem(part, cur);
            return (object)glow != null && glow.itemID == cur.itemID;
        }

        private static void UnequipLuxGlowFromSlot(PlayerCustomization pc, Part part)
        {
            if (!ResolveRefs() || (object)pc == null || (object)_unequipItem == null) return;

            try
            {
                object slotVal = Enum.ToObject(_slotEnumType, (int)part);
                object inst = _getItemInstance.Invoke(pc, new object[] { slotVal });
                if ((object)inst == null) return;

                CustomizationItem cur = _itemInstanceItem.GetValue(inst) as CustomizationItem;
                if (!IsEquippedLuxGlowItem(cur, part)) return;

                _unequipItem.Invoke(pc, new object[] { cur });
                ModLog.Debug("[LuxTint] Unequipped lux glow " + PartLabel(part) + ": " + cur.displayName);
            }
            catch (Exception ex)
            {
                ModLog.Warn("[LuxTint] Unequip lux glow: " + ex.Message);
            }
        }

        private static void CollectGlowTargets(PlayerCustomization pc, Part part, List<GlowTarget> outList)
        {
            outList.Clear();
            if (!ResolveRefs()) return;
            CollectGlowFromSlotRoots(pc, part, outList);
        }

        private static GameObject GetSlotSpawnedPrefab(PlayerCustomization pc, Part part)
        {
            if (!ResolveRefs() || (object)pc == null) return null;
            try
            {
                object slotVal = Enum.ToObject(_slotEnumType, (int)part);
                object inst = _getItemInstance.Invoke(pc, new object[] { slotVal });
                if ((object)inst == null) return null;
                return _itemInstancePrefab.GetValue(inst) as GameObject;
            }
            catch
            {
                return null;
            }
        }

        private static void CollectGlowFromSlotRoots(PlayerCustomization pc, Part part, List<GlowTarget> outList)
        {
            try
            {
                object slotVal = Enum.ToObject(_slotEnumType, (int)part);
                object inst = _getItemInstance.Invoke(pc, new object[] { slotVal });
                CustomizationItem cur = null;
                bool isLux = false;
                if ((object)inst != null)
                {
                    cur = _itemInstanceItem.GetValue(inst) as CustomizationItem;
                    isLux = IsExtraordinary(cur);
                    GameObject go = _itemInstancePrefab.GetValue(inst) as GameObject;
                    if (UnityNull.Alive(go))
                        AddGlowFromRoot(go, outList, part, isLux);
                }

                if (part == Part.Bike)
                {
                    Transform bikeModel = FindChildRecursive(pc.transform, "BikeModel");
                    if (UnityNull.Alive(bikeModel))
                        AddGlowFromRoot(bikeModel.gameObject, outList, Part.Bike, true);
                }
            }
            catch (Exception ex)
            {
                ModLog.Warn("[LuxTint] CollectGlowFromSlotRoots: " + ex.Message);
            }
        }

        private static void RestoreSavedItem(Part part)
        {
            if (!ResolveRefs()) return;
            int i = (int)part;
            int savedId = _savedItemId[i];
            if (savedId == NoSavedItem
                && (object)_savedEntries[i] != null
                && _savedEntries[i].Value != NoSavedItem)
            {
                savedId = _savedEntries[i].Value;
                _savedItemId[i] = savedId;
            }
            if (savedId == NoSavedItem) return;

            CustomizationManager cm = UnityEngine.Object.FindObjectOfType<CustomizationManager>();
            if ((object)cm == null) return;

            CustomizationItem saved = cm.GetItemFromID(savedId);
            if ((object)saved == null)
            {
                ModLog.Warn("[LuxTint] Saved item id " + savedId + " not found for " + PartLabel(part));
                return;
            }

            // Keep _savedItemId until restore succeeds so a failed equip can retry.
            _pendingRestoreId[i] = savedId;
        }

        private static IEnumerator ForceEquipCoroutine(PlayerCustomization pc, Part part, CustomizationItem item)
        {
            if ((object)item == null || (object)pc == null) yield break;

            object slotVal = Enum.ToObject(_slotEnumType, (int)part);
            object inst = _getItemInstance.Invoke(pc, new object[] { slotVal });
            if ((object)inst != null && (object)_unequipItem != null)
            {
                CustomizationItem cur = _itemInstanceItem.GetValue(inst) as CustomizationItem;
                if ((object)cur != null && cur != item)
                {
                    try { _unequipItem.Invoke(pc, new object[] { cur }); }
                    catch { }
                    for (int w = 0; w < 20; w++)
                        yield return null;
                }
            }

            IEnumerator equipRoutine = null;
            try
            {
                if ((object)_equipItemRoutine != null)
                    equipRoutine = _equipItemRoutine.Invoke(pc, new object[] { item, true }) as IEnumerator;
                else if ((object)_equipItem != null)
                    _equipItem.Invoke(pc, new object[] { item, true });
            }
            catch (Exception ex)
            {
                ModLog.Warn("[LuxTint] Equip: " + ex.Message);
            }

            if ((object)equipRoutine != null)
            {
                while (equipRoutine.MoveNext())
                    yield return equipRoutine.Current;
            }

            for (int w = 0; w < 60; w++)
            {
                inst = _getItemInstance.Invoke(pc, new object[] { slotVal });
                if ((object)inst != null)
                {
                    CustomizationItem cur = _itemInstanceItem.GetValue(inst) as CustomizationItem;
                    GameObject go = _itemInstancePrefab.GetValue(inst) as GameObject;
                    if ((object)cur != null && cur.itemID == item.itemID && UnityNull.Alive(go))
                        break;
                }
                yield return null;
            }

            if (part == Part.Bike && (object)_refreshBikeMesh != null)
                try { _refreshBikeMesh.Invoke(pc, null); }
                catch { }

            ModLog.Debug("[LuxTint] Equipped " + PartLabel(part) + ": " + item.displayName);
        }

        private static void ScheduleRestore(Part part)
        {
            MelonCoroutines.Start(RestorePartRoutine(part));
        }

        private static IEnumerator RestorePartRoutine(Part part)
        {
            int pi = (int)part;
            int wantId = _pendingRestoreId[pi];

            for (int frame = 0; frame < 30; frame++)
            {
                if (IsPartEnabled(part)) break;
                yield return null;
            }

            if (!IsPartEnabled(part))
            {
                PlayerCustomization pc = GetLocalCustomization();
                if ((object)pc != null)
                {
                    ReleaseGlowOnSlotRoots(pc, part);

                    CustomizationItem want = null;
                    CustomizationManager cm = UnityEngine.Object.FindObjectOfType<CustomizationManager>();
                    if (wantId != NoSavedItem && (object)cm != null)
                        want = cm.GetItemFromID(wantId);

                    if ((object)want != null)
                    {
                        // Exact piece captured before Lux — never invent other gear.
                        yield return ForceEquipCoroutine(pc, part, want);
                        _restoredKnownItem = true;
                        _savedItemId[pi] = NoSavedItem;
                        PersistSavedId(part);
                    }
                    else
                    {
                        // Staying on current (often Lux) gear — re-equip same item so
                        // materials respawn at stock brightness, then unfreeze HueAnimate.
                        CustomizationItem cur = null;
                        try
                        {
                            object slotVal = Enum.ToObject(_slotEnumType, (int)part);
                            object inst = _getItemInstance.Invoke(pc, new object[] { slotVal });
                            if ((object)inst != null)
                                cur = _itemInstanceItem.GetValue(inst) as CustomizationItem;
                        }
                        catch { }

                        if ((object)cur != null && IsEquippedLuxGlowItem(cur, part))
                        {
                            if ((object)_unequipItem != null)
                            {
                                try { _unequipItem.Invoke(pc, new object[] { cur }); }
                                catch { }
                                for (int w = 0; w < 10; w++)
                                    yield return null;
                            }
                            yield return ForceEquipCoroutine(pc, part, cur);
                            // Fresh Lux prefab materials — only unfreeze HueAnimate;
                            // do not scrub emission (that would kill stock glow).
                            GameObject fresh = GetSlotSpawnedPrefab(pc, part);
                            if (UnityNull.Alive(fresh))
                                UnfreezeAllHueAnimatesUnder(fresh);
                            if (part == Part.Bike)
                            {
                                Transform bikeModel = FindChildRecursive(pc.transform, "BikeModel");
                                if (UnityNull.Alive(bikeModel))
                                    UnfreezeAllHueAnimatesUnder(bikeModel.gameObject);
                            }
                        }
                        else
                        {
                            ReleaseGlowOnSlotRoots(pc, part);
                        }

                        ModLog.Debug("[LuxTint] No pre-Lux " + PartLabel(part)
                            + " captured — reset glow on current item.");
                    }

                    for (int frame = 0; frame < 20; frame++)
                    {
                        if (IsPartEnabled(part)) break;
                        yield return null;
                    }

                    if (!IsPartEnabled(part))
                    {
                        ReleaseGlowOnSlotRoots(pc, part);
                        GameObject slotRoot = GetSlotSpawnedPrefab(pc, part);
                        if (UnityNull.Alive(slotRoot))
                            UnfreezeAllHueAnimatesUnder(slotRoot);
                    }
                }
            }

            _pendingRestoreId[pi] = NoSavedItem;
        }

        private static void UnfreezeAllHueAnimatesUnder(GameObject root)
        {
            if (!UnityNull.Alive(root)) return;
            HueAnimate[] hues = root.GetComponentsInChildren<HueAnimate>(true);
            if ((object)hues == null) return;
            for (int i = 0; i < hues.Length; i++)
            {
                HueAnimate ha = hues[i];
                if ((object)ha == null) continue;
                RestoreHueAnimate(ha);
            }
        }

        private static void AddGlowFromRoot(GameObject root, List<GlowTarget> outList, Part part, bool equippedIsLux)
        {
            if (!UnityNull.Alive(root)) return;

            HueAnimate[] hues = root.GetComponentsInChildren<HueAnimate>(true);
            if ((object)hues != null)
            {
                for (int i = 0; i < hues.Length; i++)
                {
                    HueAnimate ha = hues[i];
                    if ((object)ha == null) continue;
                    Renderer rend = ha.GetComponent<Renderer>();
                    if (!UnityNull.Alive(rend))
                        rend = ha.GetComponentInChildren<Renderer>(true);
                    if (!UnityNull.Alive(rend)) continue;
                    AddGlowRenderer(rend, ha, outList, false);
                }
            }

            if (equippedIsLux && (part == Part.Torso || part == Part.Legs || part == Part.Bike))
            {
                SkinnedMeshRenderer[] smrs = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                if ((object)smrs != null)
                {
                    for (int i = 0; i < smrs.Length; i++)
                    {
                        SkinnedMeshRenderer smr = smrs[i];
                        if (!UnityNull.Alive(smr)) continue;
                        Material[] shared = smr.sharedMaterials;
                        if ((object)shared == null || shared.Length < 2) continue;
                        HueAnimate ha = smr.GetComponent<HueAnimate>();
                        AddGlowRenderer(smr, ha, outList, false);
                    }
                }
            }

            if (outList.Count == 0 && (equippedIsLux || part == Part.Bike))
                AddLuxDecalRenderers(root, outList);
        }

        private static Renderer GetRendererForHueAnimate(HueAnimate ha)
        {
            if ((object)ha == null) return null;
            Renderer rend = ha.GetComponent<Renderer>();
            if (UnityNull.Alive(rend)) return rend;
            return ha.GetComponentInChildren<Renderer>(true);
        }

        private static void AddLuxDecalRenderers(GameObject root, List<GlowTarget> outList)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if ((object)renderers == null) return;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer rend = renderers[i];
                if (!UnityNull.Alive(rend)) continue;
                if (!LooksLikeLuxDecal(rend)) continue;
                AddGlowFromRenderer(rend, outList, false);
            }
        }

        private static bool LooksLikeLuxDecal(Renderer rend)
        {
            if ((object)rend.GetComponent<HueAnimate>() != null) return true;
            string n = rend.gameObject.name.ToLowerInvariant();
            if (n.Contains("lux") || n.Contains("glow") || n.Contains("stripe")
                || n.Contains("emiss") || n.Contains("extra") || n.Contains("neon"))
                return true;

            return false;
        }

        private static void AddGlowRenderer(Renderer rend, HueAnimate ha, List<GlowTarget> outList, bool useMaterialGlow)
        {
            if (!UnityNull.Alive(rend)) return;
            for (int i = 0; i < outList.Count; i++)
            {
                if (outList[i].Renderer == rend)
                {
                    if (useMaterialGlow)
                        outList[i] = new GlowTarget
                        {
                            Renderer = rend,
                            Hue = ha != null ? ha : outList[i].Hue,
                            UseMaterialGlow = true
                        };
                    return;
                }
            }
            outList.Add(new GlowTarget
            {
                Renderer = rend,
                Hue = ha,
                UseMaterialGlow = useMaterialGlow
            });
        }

        private static void AddGlowFromRenderer(Renderer rend, List<GlowTarget> outList, bool forceMaterialGlow)
        {
            if (!UnityNull.Alive(rend)) return;

            Material[] shared = rend.sharedMaterials;
            if ((object)shared == null) return;

            HueAnimate ha = rend.GetComponent<HueAnimate>();
            bool hasEmission = false;

            for (int m = 0; m < shared.Length; m++)
            {
                Material mat = shared[m];
                if ((object)mat == null) continue;
                if (mat.IsKeywordEnabled("_EMISSION"))
                {
                    hasEmission = true;
                    break;
                }
                if (mat.HasProperty("_EmissionColor"))
                {
                    Color e = mat.GetColor("_EmissionColor");
                    if (e.maxColorComponent > 0.01f)
                        hasEmission = true;
                }
            }

            if (!forceMaterialGlow && !hasEmission && (object)ha == null) return;

            bool useMaterial = forceMaterialGlow || !hasEmission;
            AddGlowRenderer(rend, ha, outList, useMaterial);
        }

        private static Transform FindChildRecursive(Transform root, string name)
        {
            if (!UnityNull.Alive(root)) return null;
            if (string.Equals(root.name, name, StringComparison.Ordinal)) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform hit = FindChildRecursive(root.GetChild(i), name);
                if (UnityNull.Alive(hit)) return hit;
            }
            return null;
        }

        private static void ScheduleRefresh(Part part)
        {
            MelonCoroutines.Start(RefreshPartRoutine(part));
        }

        private static IEnumerator RefreshPartRoutine(Part part)
        {
            for (int frame = 0; frame < 60; frame++)
            {
                if (!IsPartEnabled(part)) break;
                if (frame == 0)
                {
                    PlayerCustomization pc = GetLocalCustomization();
                    if ((object)pc != null
                        && (part == Part.Bike || part == Part.Torso || part == Part.Legs)
                        && !IsLuxItemEquipped(pc, part))
                        EnsureGlowItemEquipped(part);
                }
                if (frame == 0 || frame % 6 == 0)
                    ApplyGlow(part);
                else
                    ReapplyCachedGlow(part);
                yield return null;
            }
        }

        private static PlayerCustomization GetLocalCustomization()
        {
            if (UnityNull.Alive(_pcCache))
                return _pcCache;
            GameObject player = PlayerCache.PlayerHuman;
            if (!UnityNull.Alive(player)) return null;
            _pcCache = player.GetComponent<PlayerCustomization>();
            return _pcCache;
        }

        private static bool ResolveRefs()
        {
            if (_refsResolved) return (object)_getItemInstance != null;

            try
            {
                Type[] types = typeof(PlayerCustomization).Assembly.GetTypes();
                for (int i = 0; i < types.Length; i++)
                {
                    if (string.Equals(types[i].Name, "mFWXh}~", StringComparison.Ordinal))
                    {
                        _slotEnumType = types[i];
                        break;
                    }
                }

                if ((object)_slotEnumType == null)
                {
                    ModLog.Warn("[LuxTint] Slot enum not found.");
                    return false;
                }

                _getItemInstance = typeof(PlayerCustomization).GetMethod("GetItemInstanceInSlot", Flags);
                _equipItem = typeof(PlayerCustomization).GetMethod("EquipItem", Flags);
                _equipItemRoutine = typeof(PlayerCustomization).GetMethod("EquipItemRoutine", Flags);
                _unequipItem = typeof(PlayerCustomization).GetMethod("UnequipItem", Flags);
                _linksField = typeof(PlayerCustomization).GetField("TXOFbp`", Flags);
                if ((object)_linksField == null)
                {
                    FieldInfo[] pcFields = typeof(PlayerCustomization).GetFields(Flags);
                    for (int fi = 0; fi < pcFields.Length; fi++)
                    {
                        if (pcFields[fi].FieldType.Equals(typeof(CustomizationLink[])))
                        {
                            _linksField = pcFields[fi];
                            break;
                        }
                    }
                }

                _getItemsOfSlot = typeof(CustomizationManager).GetMethod("GetItemsOfSlot", Flags);
                _isCustomGear = typeof(CustomizationManager).GetMethod("IsCustomGear", Flags);
                _getLinkForSlot = typeof(PlayerCustomization).GetMethod("GetLinkForSlot", Flags);
                _linkSpawnParent = typeof(CustomizationLink).GetField("spawnParent", Flags);
                _refreshBikeMesh = typeof(PlayerCustomization).GetMethod("RefreshBikeMesh", Flags);

                Type instType = typeof(ItemInstance);
                _itemInstanceItem = instType.GetField("item", Flags);
                _itemInstancePrefab = instType.GetField("spawnedPrefab", Flags);

                _refsResolved = (object)_getItemInstance != null
                    && (object)_equipItem != null
                    && (object)_linksField != null;

                return _refsResolved;
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[LuxTint] ResolveRefs: " + ex.Message);
                return false;
            }
        }

        private static string PartLabel(Part part)
        {
            switch (part)
            {
                case Part.Bike: return "Bike";
                case Part.Head: return "Helmet";
                case Part.Torso: return "Jersey";
                case Part.Legs: return "Pants";
                case Part.Eyes: return "Goggles";
                default: return part.ToString();
            }
        }

        public static void ApplyPatch(HarmonyLib.Harmony harmony)
        {
            try
            {
                MethodInfo update = typeof(HueAnimate).GetMethod("Update",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                MethodInfo prefix = typeof(LuxGlowTintHueAnimatePatch).GetMethod("Prefix",
                    BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                if ((object)update != null && (object)prefix != null)
                    harmony.Patch(update, prefix: new HarmonyLib.HarmonyMethod(prefix));
                ModLog.Debug("[LuxTint] HueAnimate patch registered.");
            }
            catch (Exception ex)
            {
                ModLog.Warn("[LuxTint] HueAnimate patch: " + ex.Message);
            }
        }
    }
}
