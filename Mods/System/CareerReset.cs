using MelonLoader;
using System;
using System.Reflection;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    /// <summary>
    /// Complete Missions / Level Reset / Sponsor Reset.
    ///
    /// BACKGROUND (confirmed against the post-update decompile, 2026-07-21):
    /// The game's own dev cheats on MissionsManager - CompeteAllMissionsCheat(),
    /// ResetMission(string), ResetCompletionData(), UnclaimAllRewards() - are now
    /// empty method bodies. Not virtual template stubs, just gutted concrete methods.
    /// GameData.GetSponsorProgress() is also hardcoded to "=> 0". This is why players
    /// report missions "not working" - it isn't a DescendersSandbox compatibility
    /// break, the game's own cheat entry points were stripped in this build.
    ///
    /// The underlying completion pathway is NOT stubbed, so everything below drives
    /// that directly instead of relying on the gutted convenience wrappers:
    ///   - MissionsManager.SetMissionComplete(MissionData)   - fully functional
    ///   - MissionsManager.Reload()                          - fully functional
    ///   - GameData.SetTeamDivision(cyFLnlM) / GetTeamDivision() - fully functional,
    ///     persists via PrefsManager key "SPONSORDIVISION"
    ///   - PrefsManager key "TOTALREP" - confirmed live in UI_SponsorOffice's own
    ///     tier-unlock comparison against SponsorProgressCard.Tier.numQuests
    ///
    /// MissionsManager, MissionGroup, MissionData, GameData and SponsorProgressCard
    /// are all public, clean-named types (only some of their MEMBERS are obfuscated),
    /// so this file uses direct typed calls wherever the member is clean, and
    /// reflection - discovered by type, not by hardcoded name, per the project's own
    /// "how to find it after re-obfuscation" convention - only for the handful of
    /// members that are still obfuscated.
    /// </summary>
    public static class CareerReset
    {
        /// <summary>Short human-readable result of the last action run, for UI feedback.</summary>
        public static string LastResult { get; private set; } = "";

        // ── Complete All Missions ───────────────────────────────────────
        public static void CompleteAllMissions()
        {
            string oldStyleMsg = null, nodeMsg = null, sponsorInterestMsg = null, sponsorTierMsg = null;

            try { oldStyleMsg = TryCompleteOldStyleMissions(); }
            catch (Exception ex) { MelonLogger.Error("[CareerReset] (legacy) crashed: " + ex.Message); }

            try { nodeMsg = TryCompleteViaProgressNodes(); }
            catch (Exception ex) { MelonLogger.Error("[CareerReset] (nodes) crashed: " + ex.Message); }

            try { sponsorInterestMsg = TryCompleteSponsorInterestNodes(); }
            catch (Exception ex) { MelonLogger.Error("[CareerReset] (sponsor interest) crashed: " + ex.Message); }

            try { sponsorTierMsg = TryMaxSponsorTier(); }
            catch (Exception ex) { MelonLogger.Error("[CareerReset] (sponsor tier) crashed: " + ex.Message); }

            string combined = "";
            if (oldStyleMsg != null) combined += (combined.Length > 0 ? "; " : "") + oldStyleMsg;
            if (nodeMsg != null) combined += (combined.Length > 0 ? "; " : "") + nodeMsg;
            if (sponsorInterestMsg != null) combined += (combined.Length > 0 ? "; " : "") + sponsorInterestMsg;
            if (sponsorTierMsg != null) combined += (combined.Length > 0 ? "; " : "") + sponsorTierMsg;
            if (combined.Length == 0) combined = "Nothing to complete - see log";

            MelonLogger.Msg("[CareerReset] Complete All Missions finished: " + combined);
            LastResult = combined;
        }

        /// <summary>Legacy MissionsManager/MissionGroup/MissionData path. Returns a short status
        /// string, or null if MissionsManager wasn't found or held no groups. On the live build
        /// this comes back empty - see TryCompleteViaProgressNodes for the system that's actually
        /// populated. Left in place in case a future update repopulates it.</summary>
        private static string TryCompleteOldStyleMissions()
        {
            MissionsManager mm = UnityEngine.Object.FindObjectOfType<MissionsManager>();
            if ((object)mm == null)
            {
                MelonLogger.Msg("[CareerReset] (legacy) MissionsManager not found in scene.");
                return null;
            }

            MissionGroup[] groups = GetMissionGroups(mm);
            if (groups == null || groups.Length == 0)
            {
                MelonLogger.Msg("[CareerReset] (legacy) MissionsManager found but holds no mission "
                    + "groups - this system looks unused in the current build.");
                return null;
            }

            int completedCount = 0, alreadyDoneCount = 0, totalMissions = 0, groupsTouched = 0;

            for (int g = 0; g < groups.Length; g++)
            {
                MissionGroup group = groups[g];
                if ((object)group == null) continue;
                groupsTouched++;

                MissionData[] missions = group.Missions;
                if (missions == null) continue;

                for (int i = 0; i < missions.Length; i++)
                {
                    MissionData md = missions[i];
                    if ((object)md == null) continue;
                    totalMissions++;

                    if (md.IsComplete) { alreadyDoneCount++; continue; }

                    try
                    {
                        mm.SetMissionComplete(md);
                        completedCount++;
                    }
                    catch (Exception ex)
                    {
                        MelonLogger.Warning("[CareerReset] (legacy) SetMissionComplete failed for mission '"
                            + SafeId(md) + "': " + ex.Message);
                    }
                }
            }

            MelonLogger.Msg("[CareerReset] (legacy) " + completedCount + " newly completed, "
                + alreadyDoneCount + " already done, " + totalMissions + " total mission(s) across "
                + groupsTouched + " group(s).");
            if (totalMissions == 0) return null;
            return completedCount + " legacy mission(s) completed";
        }

        /// <summary>The system that's actually live in this build: PlayerProgress.progressNodes,
        /// gated by the "TOTALREP" reputation counter. This is what shows up in-game as "nodes" -
        /// confirmed by reading PlayerProgress.cs, which walks progressNodes comparing the player's
        /// reputation against each ProgressNode.reputationNeeded, and GameData, which holds the
        /// live PlayerProgress reference. Completing everything means pushing reputation past the
        /// highest threshold in the list.</summary>
        private static string TryCompleteViaProgressNodes()
        {
            GameData gd = UnityEngine.Object.FindObjectOfType<GameData>();
            if ((object)gd == null)
            {
                MelonLogger.Warning("[CareerReset] (nodes) GameData not found in scene.");
                return null;
            }

            FieldInfo ppField = FindFieldByType(typeof(GameData), typeof(PlayerProgress),
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if ((object)ppField == null)
            {
                MelonLogger.Warning("[CareerReset] (nodes) No PlayerProgress field found on GameData.");
                return null;
            }

            PlayerProgress pp = ppField.GetValue(gd) as PlayerProgress;
            if ((object)pp == null)
            {
                MelonLogger.Warning("[CareerReset] (nodes) PlayerProgress field on GameData was null.");
                return null;
            }

            ProgressNode[] nodes = pp.progressNodes;
            if (nodes == null || nodes.Length == 0)
            {
                MelonLogger.Warning("[CareerReset] (nodes) progressNodes array was empty.");
                return null;
            }

            int maxRep = 0;
            for (int i = 0; i < nodes.Length; i++)
            {
                if ((object)nodes[i] == null) continue;
                if (nodes[i].reputationNeeded > maxRep) maxRep = nodes[i].reputationNeeded;
            }

            object prefsInstance = FindSingletonInstance(typeof(PrefsManager));
            PrefsManager prefs = prefsInstance as PrefsManager;
            if ((object)prefs == null)
            {
                MelonLogger.Warning("[CareerReset] (nodes) Could not resolve a PrefsManager instance.");
                return null;
            }

            int target = maxRep + 1000;
            int before = prefs.GetInt("TOTALREP");
            prefs.SetInt("TOTALREP", target);
            try { prefs.Save(); }
            catch (Exception exSave) { MelonLogger.Warning("[CareerReset] (nodes) prefs.Save() threw: " + exSave.Message); }

            MelonLogger.Msg("[CareerReset] (nodes) " + nodes.Length + " progress node(s) found, highest needs "
                + maxRep + " reputation. TOTALREP: " + before + " -> " + target + ".");
            return nodes.Length + " progress node(s) unlocked (reputation set to " + target + ")";
        }

        /// <summary>Pre-sponsorship "team node" interest gate - a third system, separate from both
        /// the legacy missions and the reputation/progress-node track. Confirmed directly in
        /// UI_SponsorOffice's own unsponsored-state code: it reads PrefsManager ints "SPONSOR_1",
        /// "SPONSOR_2", "SPONSOR_3" (one per brand), renders each as "X/3", and marks a brand
        /// interested once its counter reaches 3. This is what the "Sponsor Interest" screen
        /// actually reads - TOTALREP never touches it.</summary>
        private static string TryCompleteSponsorInterestNodes()
        {
            object prefsInstance = FindSingletonInstance(typeof(PrefsManager));
            PrefsManager prefs = prefsInstance as PrefsManager;
            if ((object)prefs == null)
            {
                MelonLogger.Warning("[CareerReset] (sponsor interest) Could not resolve a PrefsManager instance.");
                return null;
            }

            int changed = 0;
            for (int i = 1; i <= 3; i++)
            {
                string key = "SPONSOR_" + i;
                int before = prefs.GetInt(key);
                if (before < 3)
                {
                    prefs.SetInt(key, 3);
                    changed++;
                    MelonLogger.Msg("[CareerReset] (sponsor interest) " + key + ": " + before + " -> 3.");
                }
            }

            try { prefs.Save(); }
            catch (Exception exSave) { MelonLogger.Warning("[CareerReset] (sponsor interest) prefs.Save() threw: " + exSave.Message); }

            if (changed == 0) return null;
            return changed + " sponsor track(s) marked interested (3/3)";
        }

        /// <summary>Maxes out the CURRENT sponsor's tier progress (the "Enemy Novice Goggles" /
        /// reward-line circles screen). UI_SponsorOffice actually has two near-identical tier
        /// refresh methods here - one obfuscated, reading "TOTALREP" (a decoy: never moved the
        /// circles when tested), and the real one, the clean-named InitializeTeamProgression(),
        /// which is the one actually wired to the sponsored-state UI and reads a different
        /// counter entirely: "TEAMTASKSCOMPLETED". This drives that one directly.</summary>
        public static void MaxSponsorLevel()
        {
            string msg = null;
            try { msg = TryMaxSponsorTier(); }
            catch (Exception ex) { MelonLogger.Error("[CareerReset] (sponsor tier) crashed: " + ex.Message); }
            LastResult = msg ?? "Nothing to raise - see log";
        }

        private static string TryMaxSponsorTier()
        {
            object prefsInstance = FindSingletonInstance(typeof(PrefsManager));
            PrefsManager prefs = prefsInstance as PrefsManager;
            if ((object)prefs == null)
            {
                MelonLogger.Warning("[CareerReset] (sponsor tier) Could not resolve a PrefsManager instance.");
                return null;
            }

            int before = prefs.GetInt("TEAMTASKSCOMPLETED");
            if (before >= 999) return null;

            prefs.SetInt("TEAMTASKSCOMPLETED", 999);
            try { prefs.Save(); }
            catch (Exception exSave) { MelonLogger.Warning("[CareerReset] (sponsor tier) prefs.Save() threw: " + exSave.Message); }

            MelonLogger.Msg("[CareerReset] TEAMTASKSCOMPLETED: " + before + " -> 999. "
                + "Leave and re-enter the Sponsor Office screen to see it refresh.");
            return "Sponsor tier tasks " + before + "->999";
        }

        // ── Reset Level Progress (missions + tour/group unlocks) ────────
        public static void ResetLevelProgress()
        {
            try
            {
                MissionsManager mm = UnityEngine.Object.FindObjectOfType<MissionsManager>();
                if ((object)mm == null)
                {
                    MelonLogger.Warning("[CareerReset] ResetLevelProgress: MissionsManager not found in scene.");
                    LastResult = "MissionsManager not found";
                    return;
                }

                MissionGroup[] groups = GetMissionGroups(mm);
                int clearedCount = 0, totalMissions = 0;

                if (groups != null)
                {
                    for (int g = 0; g < groups.Length; g++)
                    {
                        MissionGroup group = groups[g];
                        if ((object)group == null) continue;

                        MissionData[] missions = group.Missions;
                        if (missions == null) continue;

                        for (int i = 0; i < missions.Length; i++)
                        {
                            MissionData md = missions[i];
                            if ((object)md == null) continue;
                            totalMissions++;

                            if (md.IsComplete)
                            {
                                md.IsComplete = false;
                                clearedCount++;
                            }
                        }
                    }
                }
                else
                {
                    MelonLogger.Warning("[CareerReset] ResetLevelProgress: could not enumerate mission groups - "
                        + "live IsComplete flags were not touched, only MissionsManager.Reload() ran.");
                }

                // MissionsManager.Reload() is public, clean-named, and confirmed NOT stubbed in the
                // post-update decompile: it clears the internal completed-mission save list and the
                // claimed-group-reward list, then kicks off the manager's own reload coroutine.
                // This is the closest live equivalent to the gutted ResetCompletionData().
                try
                {
                    mm.Reload();
                    MelonLogger.Msg("[CareerReset] MissionsManager.Reload() called - internal completion/claim lists cleared.");
                }
                catch (Exception ex)
                {
                    MelonLogger.Warning("[CareerReset] mm.Reload() threw: " + ex.Message);
                }

                MelonLogger.Msg("[CareerReset] Reset Level Progress: cleared IsComplete on "
                    + clearedCount + "/" + totalMissions + " mission(s). Group/tour lock state is derived "
                    + "from mission completion, so it resets as a side effect of this.");
                MelonLogger.Msg("[CareerReset] NOTE: this resets the live session state immediately. "
                    + "If completed missions reappear after a full game restart, the persisted PrefsManager "
                    + "blob (key \"MissionsData\") needs clearing too - flag it and we'll add that next.");
                LastResult = "Cleared " + clearedCount + " mission(s)";
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[CareerReset] ResetLevelProgress: " + ex.Message);
                LastResult = "Error - see log";
            }
        }

        // ── Reset Sponsor Progress (division + reputation) ──────────────
        public static void ResetSponsorProgress()
        {
            try
            {
                GameData gd = UnityEngine.Object.FindObjectOfType<GameData>();
                if ((object)gd == null)
                {
                    MelonLogger.Warning("[CareerReset] ResetSponsorProgress: GameData not found in scene.");
                    LastResult = "GameData not found";
                    return;
                }

                cyFLnlM before = gd.GetTeamDivision();
                gd.SetTeamDivision(cyFLnlM.Novice);
                bool divisionChanged = before != cyFLnlM.Novice;
                MelonLogger.Msg("[CareerReset] Sponsor division: " + before + " -> Novice "
                    + "(persisted via PrefsManager key \"SPONSORDIVISION\").");

                // TOTALREP is the reputation/quest-progress counter UI_SponsorOffice itself reads
                // to decide which sponsor tiers show as complete/in-progress/locked (compared directly
                // against SponsorProgressCard.Tier.numQuests). Confirmed by reading that comparison in
                // the decompile, not guessed - most of the OTHER PrefsManager-adjacent int fields in
                // this build are decoy strings that don't correspond to anything real.
                object prefsInstance = FindSingletonInstance(typeof(PrefsManager));
                PrefsManager prefs = prefsInstance as PrefsManager;
                int beforeRep = -1;
                bool prefsOk = false;
                int sponsorNodesCleared = 0;
                if ((object)prefs != null)
                {
                    beforeRep = prefs.GetInt("TOTALREP");
                    prefs.SetInt("TOTALREP", 0);

                    // Mirror of TryCompleteSponsorInterestNodes - zero the pre-sponsorship
                    // "team node" counters too, so this is the true inverse of Complete All Missions.
                    for (int i = 1; i <= 3; i++)
                    {
                        string key = "SPONSOR_" + i;
                        if (prefs.GetInt(key) != 0)
                        {
                            prefs.SetInt(key, 0);
                            sponsorNodesCleared++;
                        }
                    }

                    // Mirror of TryMaxSponsorTier - this is the counter that actually drives the
                    // sponsored-state tier circles (InitializeTeamProgression), not TOTALREP.
                    int beforeTeamTasks = prefs.GetInt("TEAMTASKSCOMPLETED");
                    if (beforeTeamTasks != 0)
                    {
                        prefs.SetInt("TEAMTASKSCOMPLETED", 0);
                        sponsorNodesCleared++;
                    }

                    try { prefs.Save(); prefsOk = true; }
                    catch (Exception exSave) { MelonLogger.Warning("[CareerReset] prefs.Save() threw: " + exSave.Message); }
                    MelonLogger.Msg("[CareerReset] TOTALREP: " + beforeRep + " -> 0. TEAMTASKSCOMPLETED: " + beforeTeamTasks
                        + " -> 0. SPONSOR_1/2/3 cleared: " + sponsorNodesCleared + ".");
                }
                else
                {
                    MelonLogger.Warning("[CareerReset] Could not resolve a live PrefsManager instance - "
                        + "TOTALREP was not touched. Division reset above still applies.");
                }

                MelonLogger.Msg("[CareerReset] Reset Sponsor Progress complete.");
                if (!divisionChanged && sponsorNodesCleared == 0 && (beforeRep == 0 || !prefsOk))
                    LastResult = "Already at Novice / 0 rep / 0 sponsor nodes - nothing to reset";
                else
                    LastResult = "Division " + before + "->Novice, rep " + (beforeRep >= 0 ? beforeRep.ToString() : "?")
                        + "->0, " + sponsorNodesCleared + " sponsor node(s) cleared";
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[CareerReset] ResetSponsorProgress: " + ex.Message);
                LastResult = "Error - see log";
            }
        }

        // ══════════════════════════════════════════════════════════════
        //  Reflection helpers - discover by TYPE, never by hardcoded
        //  obfuscated name, so this keeps working across re-obfuscation.
        // ══════════════════════════════════════════════════════════════

        private static FieldInfo _groupsField;
        private static bool _groupsFieldSearched = false;

        private static MissionGroup[] GetMissionGroups(MissionsManager mm)
        {
            if (!_groupsFieldSearched)
            {
                _groupsFieldSearched = true;
                _groupsField = FindFieldByType(typeof(MissionsManager), typeof(MissionGroup[]),
                    BindingFlags.NonPublic | BindingFlags.Instance);
            }
            if ((object)_groupsField == null) return null;

            try
            {
                return _groupsField.GetValue(mm) as MissionGroup[];
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[CareerReset] Reading mission group field failed: " + ex.Message);
                return null;
            }
        }

        /// <summary>Scans an owner type's private instance fields for the (expected-unique) field of a given type.
        /// Logs the outcome either way, and dumps every candidate field if the search fails, so the correct
        /// name can be picked out of the log in one test cycle if this ever needs re-pointing.</summary>
        private static FieldInfo FindFieldByType(Type owner, Type fieldType, BindingFlags flags)
        {
            try
            {
                FieldInfo[] fields = owner.GetFields(flags);
                FieldInfo match = null;
                int matchCount = 0;
                for (int i = 0; i < fields.Length; i++)
                {
                    if (fields[i].FieldType.Equals(fieldType))
                    {
                        matchCount++;
                        if ((object)match == null) match = fields[i];
                    }
                }

                if (matchCount == 0)
                {
                    MelonLogger.Warning("[CareerReset] No field of type " + fieldType.Name
                        + " found on " + owner.Name + " (flags=" + flags + "). Dumping candidate fields for manual ID:");
                    for (int i = 0; i < fields.Length; i++)
                        MelonLogger.Msg("    " + fields[i].FieldType.Name + "  " + fields[i].Name);
                    return null;
                }
                if (matchCount > 1)
                    MelonLogger.Warning("[CareerReset] " + matchCount + " fields of type " + fieldType.Name
                        + " found on " + owner.Name + " - using the first one: " + match.Name);
                else
                    MelonLogger.Msg("[CareerReset] Found field '" + match.Name + "' (" + fieldType.Name
                        + ") on " + owner.Name + ".");

                return match;
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[CareerReset] FindFieldByType(" + owner.Name + ", " + fieldType.Name + "): " + ex.Message);
                return null;
            }
        }

        /// <summary>Resolves a singleton-style instance of type t: scene search first (covers
        /// MonoBehaviour/Component singletons), then falls back to scanning t's own public static
        /// members for one whose type matches t (covers plain-class or abstract-base singletons
        /// like PrefsManager, without needing to know the obfuscated accessor's name).</summary>
        private static object FindSingletonInstance(Type t)
        {
            if ((object)t == null) return null;
            try
            {
                if (typeof(UnityEngine.Object).IsAssignableFrom(t))
                {
                    UnityEngine.Object found = UnityEngine.Object.FindObjectOfType(t);
                    if ((object)found != null)
                    {
                        MelonLogger.Msg("[CareerReset] Found " + t.Name + " instance via FindObjectOfType.");
                        return found;
                    }
                    MelonLogger.Warning("[CareerReset] FindObjectOfType(" + t.Name + ") returned null, trying static member scan...");
                }

                PropertyInfo[] props = t.GetProperties(BindingFlags.Public | BindingFlags.Static);
                for (int i = 0; i < props.Length; i++)
                {
                    if (t.IsAssignableFrom(props[i].PropertyType))
                    {
                        object val = props[i].GetValue(null, null);
                        if ((object)val != null)
                        {
                            MelonLogger.Msg("[CareerReset] Found " + t.Name + " instance via static property '" + props[i].Name + "'.");
                            return val;
                        }
                    }
                }

                FieldInfo[] fields = t.GetFields(BindingFlags.Public | BindingFlags.Static);
                for (int i = 0; i < fields.Length; i++)
                {
                    if (t.IsAssignableFrom(fields[i].FieldType))
                    {
                        object val = fields[i].GetValue(null);
                        if ((object)val != null)
                        {
                            MelonLogger.Msg("[CareerReset] Found " + t.Name + " instance via static field '" + fields[i].Name + "'.");
                            return val;
                        }
                    }
                }

                MelonLogger.Warning("[CareerReset] Could not resolve a singleton instance for " + t.Name + ".");
                return null;
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[CareerReset] FindSingletonInstance(" + t.Name + "): " + ex.Message);
                return null;
            }
        }

        private static string SafeId(MissionData md)
        {
            try { return md.Id; } catch { return "?"; }
        }
    }
}
