using MelonLoader;
using DescendersModMenu;
using System;
using System.Reflection;
using CodeStage.AntiCheat.ObscuredTypes;
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
            catch (Exception ex) { MelonLogger.Error("[CareerReset] (legacy) crashed: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "CareerReset"); }

            try { nodeMsg = TryCompleteViaProgressNodes(); }
            catch (Exception ex) { MelonLogger.Error("[CareerReset] (nodes) crashed: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "CareerReset"); }

            try { sponsorInterestMsg = TryCompleteSponsorInterestNodes(); }
            catch (Exception ex) { MelonLogger.Error("[CareerReset] (sponsor interest) crashed: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "CareerReset"); }

            try { sponsorTierMsg = TryMaxSponsorTier(); }
            catch (Exception ex) { MelonLogger.Error("[CareerReset] (sponsor tier) crashed: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "CareerReset"); }

            string combined = "";
            if (oldStyleMsg != null) combined += (combined.Length > 0 ? "; " : "") + oldStyleMsg;
            if (nodeMsg != null) combined += (combined.Length > 0 ? "; " : "") + nodeMsg;
            if (sponsorInterestMsg != null) combined += (combined.Length > 0 ? "; " : "") + sponsorInterestMsg;
            if (sponsorTierMsg != null) combined += (combined.Length > 0 ? "; " : "") + sponsorTierMsg;
            if (combined.Length == 0) combined = "Nothing to complete - see log";

            ModLog.Debug("[CareerReset] Complete All Missions finished: " + combined);
            LastResult = combined;
        }

        /// <summary>
        /// Completes every Grand Tour challenge (Developer Tour + Encore Tour and any
        /// other Tour on GameData._tours), claims mission/group rewards, unlocks tour
        /// outfit rewards, and clears category padlocks (they unlock when prereq groups
        /// hit the 5-complete threshold — finishing everything clears them all).
        /// </summary>
        public static void CompleteGrandTour()
        {
            try
            {
                MissionsManager mm = UnityEngine.Object.FindObjectOfType<MissionsManager>();
                if ((object)mm == null)
                {
                    ModLog.Warn("[CareerReset] CompleteGrandTour: MissionsManager not found.");
                    LastResult = "MissionsManager not found - open Grand Tour once?";
                    return;
                }

                if (mm.GetTourCount() <= 0)
                {
                    ModLog.Warn("[CareerReset] CompleteGrandTour: GetTourCount()=0 - tours not loaded yet.");
                    LastResult = "Tours not loaded - open The Grand Tour menu once";
                    return;
                }

                MissionGroup[] groups = GetMissionGroups(mm);
                if (groups == null || groups.Length == 0)
                {
                    ModLog.Warn("[CareerReset] CompleteGrandTour: no MissionGroup[] on MissionsManager.");
                    LastResult = "No tour groups loaded";
                    return;
                }

                if (!ResolveMissionSaveLists(mm))
                {
                    LastResult = "Could not resolve mission save lists - see log";
                    return;
                }

                int completed = 0, alreadyDone = 0, missionClaims = 0, groupClaims = 0;
                int tourRewardSets = 0, seenMarks = 0;

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

                        if (md.IsComplete || mm.IsMissionComplete(md.Id))
                        {
                            alreadyDone++;
                        }
                        else
                        {
                            try
                            {
                                mm.SetMissionComplete(md);
                                completed++;
                            }
                            catch (Exception ex)
                            {
                                ModLog.Warn("[CareerReset] GrandTour SetMissionComplete '"
                                    + SafeId(md) + "': " + ex.Message);
                            }
                        }
                    }
                }

                for (int g = 0; g < groups.Length; g++)
                {
                    MissionGroup group = groups[g];
                    if ((object)group == null) continue;

                    MissionData[] missions = group.Missions;
                    if (missions != null)
                    {
                        for (int i = 0; i < missions.Length; i++)
                        {
                            MissionData md = missions[i];
                            if ((object)md == null || !md.IsComplete) continue;
                            if (mm.CheckIfRewardClaimed(md)) continue;

                            try
                            {
                                mm.SetRewardClaimed(md);
                                CustomizationItem[] rewards = md.MissionRewards;
                                if ((object)rewards != null && rewards.Length > 0)
                                    mm.ClaimMissionCompleteRewards(md);
                                else
                                    mm.ClaimRandomItemReward(md);
                                missionClaims++;
                            }
                            catch (Exception ex)
                            {
                                ModLog.Warn("[CareerReset] GrandTour claim mission '"
                                    + SafeId(md) + "': " + ex.Message);
                            }
                        }
                    }

                    if (!mm.IsMissionGroupComplete(group)) continue;

                    try
                    {
                        EnsureGroupRegistered(mm, group);
                        if (!mm.CheckIfRewardClaimed(group))
                        {
                            mm.SetRewardClaimed(group);
                            mm.ClaimGroupCompleteRewards(group);
                            groupClaims++;
                        }
                    }
                    catch (Exception ex)
                    {
                        ModLog.Warn("[CareerReset] GrandTour claim group '"
                            + SafeGroupId(group) + "': " + ex.Message);
                    }

                    try
                    {
                        string gid = group.Id;
                        if (!string.IsNullOrEmpty(gid) && mm.IsMissionGroupUnseen(gid))
                        {
                            mm.MarkMissionGroupAsSeen(gid);
                            seenMarks++;
                        }
                    }
                    catch { }
                }

                try { mm.TryClaimCompletedTourOneReward(); }
                catch (Exception ex)
                {
                    ModLog.Warn("[CareerReset] GrandTour TryClaimCompletedTourOneReward: " + ex.Message);
                }

                CustomizationManager cm = UnityEngine.Object.FindObjectOfType<CustomizationManager>();
                int tourCount = mm.GetTourCount();
                for (int t = 0; t < tourCount; t++)
                {
                    Tour tour = null;
                    try { tour = mm.GetTour(t); } catch { }
                    if ((object)tour == null) continue;
                    if (!IsTourFullyComplete(mm, tour)) continue;

                    CustomizationItem[] tourRewards = tour.Rewards;
                    if ((object)tourRewards == null || tourRewards.Length == 0) continue;
                    if ((object)cm == null) continue;

                    try
                    {
                        cm.UnlockItems(tourRewards, true, true);
                        tourRewardSets++;
                        ModLog.Debug("[CareerReset] GrandTour unlocked Rewards for tour \""
                            + tour.TourName + "\" (" + tourRewards.Length + " item(s)).");
                    }
                    catch (Exception ex)
                    {
                        ModLog.Warn("[CareerReset] GrandTour UnlockItems tour \""
                            + tour.TourName + "\": " + ex.Message);
                    }
                }

                string msg = "Grand Tour: +" + completed + " complete (" + alreadyDone
                    + " already), " + missionClaims + " mission reward(s), "
                    + groupClaims + " group reward(s), " + tourRewardSets
                    + " tour reward set(s)";
                if (seenMarks > 0) msg += ", " + seenMarks + " seen";
                ModLog.Feedback("[CareerReset] " + msg);
                LastResult = msg;
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[CareerReset] CompleteGrandTour: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "CareerReset");
                LastResult = "Error - see log";
            }
        }

        private static FieldInfo _missionSaveListField;
        private static FieldInfo _groupSaveListField;
        private static bool _saveListsResolved;

        private static bool ResolveMissionSaveLists(MissionsManager mm)
        {
            if (_saveListsResolved
                && (object)_missionSaveListField != null
                && (object)_groupSaveListField != null)
                return true;

            _saveListsResolved = true;
            try
            {
                FieldInfo[] fields = typeof(MissionsManager).GetFields(
                    BindingFlags.NonPublic | BindingFlags.Instance);
                System.Collections.Generic.List<FieldInfo> listFields =
                    new System.Collections.Generic.List<FieldInfo>();
                for (int i = 0; i < fields.Length; i++)
                {
                    if (fields[i].FieldType.Equals(typeof(System.Collections.Generic.List<MissionsSaveData>)))
                        listFields.Add(fields[i]);
                }

                if (listFields.Count < 2)
                {
                    ModLog.Warn("[CareerReset] Expected 2 List<MissionsSaveData> fields, found "
                        + listFields.Count);
                    return false;
                }

                // Prefer matching MissionGroup.Id entries → group list; mission Ids → mission list.
                MissionGroup[] groups = GetMissionGroups(mm);
                FieldInfo groupField = null;
                FieldInfo missionField = null;

                if ((object)groups != null)
                {
                    for (int li = 0; li < listFields.Count; li++)
                    {
                        System.Collections.Generic.List<MissionsSaveData> list =
                            listFields[li].GetValue(mm) as System.Collections.Generic.List<MissionsSaveData>;
                        if ((object)list == null || list.Count == 0) continue;

                        int groupHits = 0, missionHits = 0;
                        for (int g = 0; g < groups.Length; g++)
                        {
                            MissionGroup grp = groups[g];
                            if ((object)grp == null || string.IsNullOrEmpty(grp.Id)) continue;
                            for (int e = 0; e < list.Count; e++)
                            {
                                if ((object)list[e] == null) continue;
                                if (string.Equals(list[e].Id, grp.Id, StringComparison.Ordinal))
                                    groupHits++;
                            }

                            MissionData[] mds = grp.Missions;
                            if (mds == null) continue;
                            for (int mi = 0; mi < mds.Length; mi++)
                            {
                                if ((object)mds[mi] == null || string.IsNullOrEmpty(mds[mi].Id)) continue;
                                for (int e = 0; e < list.Count; e++)
                                {
                                    if ((object)list[e] == null) continue;
                                    if (string.Equals(list[e].Id, mds[mi].Id, StringComparison.Ordinal))
                                        missionHits++;
                                }
                            }
                        }

                        if (groupHits > missionHits && (object)groupField == null)
                            groupField = listFields[li];
                        if (missionHits > groupHits && (object)missionField == null)
                            missionField = listFields[li];
                    }
                }

                // Fallback: complete one incomplete mission and see which list grows.
                if ((object)groupField == null || (object)missionField == null)
                {
                    int c0 = CountSaveList(listFields[0].GetValue(mm));
                    int c1 = CountSaveList(listFields[1].GetValue(mm));
                    MissionData probe = FindIncompleteMission(groups);
                    if ((object)probe != null)
                    {
                        mm.SetMissionComplete(probe);
                        int n0 = CountSaveList(listFields[0].GetValue(mm));
                        int n1 = CountSaveList(listFields[1].GetValue(mm));
                        if (n0 > c0)
                        {
                            missionField = listFields[0];
                            groupField = listFields[1];
                        }
                        else if (n1 > c1)
                        {
                            missionField = listFields[1];
                            groupField = listFields[0];
                        }
                    }
                }

                if ((object)groupField == null || (object)missionField == null)
                {
                    // Last resort: SetMissionComplete writes Hi~yQqh first in field order
                    // is unreliable — assign uniquely remaining.
                    if ((object)missionField == null && (object)groupField != null)
                    {
                        for (int i = 0; i < listFields.Count; i++)
                            if ((object)listFields[i] != (object)groupField)
                            { missionField = listFields[i]; break; }
                    }
                    if ((object)groupField == null && (object)missionField != null)
                    {
                        for (int i = 0; i < listFields.Count; i++)
                            if ((object)listFields[i] != (object)missionField)
                            { groupField = listFields[i]; break; }
                    }
                    if ((object)groupField == null)
                    {
                        missionField = listFields[0];
                        groupField = listFields[1];
                        ModLog.Warn("[CareerReset] GrandTour save-list identity uncertain — using field order.");
                    }
                }

                _missionSaveListField = missionField;
                _groupSaveListField = groupField;
                ModLog.Debug("[CareerReset] GrandTour lists: missions='" + missionField.Name
                    + "' groups='" + groupField.Name + "'");
                return true;
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[CareerReset] ResolveMissionSaveLists: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "CareerReset");
                return false;
            }
        }

        private static int CountSaveList(object listObj)
        {
            System.Collections.Generic.List<MissionsSaveData> list =
                listObj as System.Collections.Generic.List<MissionsSaveData>;
            return (object)list != null ? list.Count : -1;
        }

        private static MissionData FindIncompleteMission(MissionGroup[] groups)
        {
            if ((object)groups == null) return null;
            for (int g = 0; g < groups.Length; g++)
            {
                if ((object)groups[g] == null || groups[g].Missions == null) continue;
                MissionData[] mds = groups[g].Missions;
                for (int i = 0; i < mds.Length; i++)
                {
                    if ((object)mds[i] != null && !mds[i].IsComplete)
                        return mds[i];
                }
            }
            return null;
        }

        private static void EnsureGroupRegistered(MissionsManager mm, MissionGroup group)
        {
            if ((object)_groupSaveListField == null || (object)group == null) return;
            string id = group.Id;
            if (string.IsNullOrEmpty(id)) return;

            System.Collections.Generic.List<MissionsSaveData> list =
                _groupSaveListField.GetValue(mm) as System.Collections.Generic.List<MissionsSaveData>;
            if ((object)list == null) return;

            for (int i = 0; i < list.Count; i++)
            {
                if ((object)list[i] != null
                    && string.Equals(list[i].Id, id, StringComparison.Ordinal))
                    return;
            }

            MissionsSaveData entry = new MissionsSaveData();
            entry.Id = id;
            entry.RewardClaimed = false;
            list.Add(entry);
        }

        private static bool IsTourFullyComplete(MissionsManager mm, Tour tour)
        {
            if ((object)tour == null) return false;
            MissionGroup[] tGroups = tour.MissionGroups;
            if ((object)tGroups == null || tGroups.Length == 0) return false;
            for (int i = 0; i < tGroups.Length; i++)
            {
                if ((object)tGroups[i] == null) continue;
                if (!mm.IsMissionGroupComplete(tGroups[i])) return false;
            }
            return true;
        }

        private static string SafeGroupId(MissionGroup group)
        {
            try
            {
                if ((object)group == null) return "?";
                string id = group.Id;
                return string.IsNullOrEmpty(id) ? "?" : id;
            }
            catch { return "?"; }
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
                ModLog.Debug("[CareerReset] (legacy) MissionsManager not found in scene.");
                return null;
            }

            MissionGroup[] groups = GetMissionGroups(mm);
            if (groups == null || groups.Length == 0)
            {
                ModLog.Debug("[CareerReset] (legacy) MissionsManager found but holds no mission "
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
                        ModLog.Warn("[CareerReset] (legacy) SetMissionComplete failed for mission '"
                            + SafeId(md) + "': " + ex.Message);
                    }
                }
            }

            ModLog.Debug("[CareerReset] (legacy) " + completedCount + " newly completed, "
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
                ModLog.Warn("[CareerReset] (nodes) GameData not found in scene.");
                return null;
            }

            FieldInfo ppField = FindFieldByType(typeof(GameData), typeof(PlayerProgress),
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if ((object)ppField == null)
            {
                ModLog.Warn("[CareerReset] (nodes) No PlayerProgress field found on GameData.");
                return null;
            }

            PlayerProgress pp = ppField.GetValue(gd) as PlayerProgress;
            if ((object)pp == null)
            {
                ModLog.Warn("[CareerReset] (nodes) PlayerProgress field on GameData was null.");
                return null;
            }

            ProgressNode[] nodes = pp.progressNodes;
            if (nodes == null || nodes.Length == 0)
            {
                ModLog.Warn("[CareerReset] (nodes) progressNodes array was empty.");
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
                ModLog.Warn("[CareerReset] (nodes) Could not resolve a PrefsManager instance.");
                return null;
            }

            int target = maxRep + 1000;
            int before = prefs.GetInt("TOTALREP");
            prefs.SetInt("TOTALREP", target);
            try { prefs.Save(); }
            catch (Exception exSave) { ModLog.Warn("[CareerReset] (nodes) prefs.Save() threw: " + exSave.Message); }

            ModLog.Debug("[CareerReset] (nodes) " + nodes.Length + " progress node(s) found, highest needs "
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
                ModLog.Warn("[CareerReset] (sponsor interest) Could not resolve a PrefsManager instance.");
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
                    ModLog.Feedback("[CareerReset] (sponsor interest) " + key + ": " + before + " -> 3.");
                }
            }

            try { prefs.Save(); }
            catch (Exception exSave) { ModLog.Warn("[CareerReset] (sponsor interest) prefs.Save() threw: " + exSave.Message); }

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
            catch (Exception ex) { MelonLogger.Error("[CareerReset] (sponsor tier) crashed: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "CareerReset"); }
            LastResult = msg ?? "Nothing to raise - see log";
        }

        private static string TryMaxSponsorTier()
        {
            object prefsInstance = FindSingletonInstance(typeof(PrefsManager));
            PrefsManager prefs = prefsInstance as PrefsManager;
            if ((object)prefs == null)
            {
                ModLog.Warn("[CareerReset] (sponsor tier) Could not resolve a PrefsManager instance.");
                return null;
            }

            int before = prefs.GetInt("TEAMTASKSCOMPLETED");
            if (before >= 999) return null;

            prefs.SetInt("TEAMTASKSCOMPLETED", 999);
            try { prefs.Save(); }
            catch (Exception exSave) { ModLog.Warn("[CareerReset] (sponsor tier) prefs.Save() threw: " + exSave.Message); }

            ModLog.Feedback("[CareerReset] TEAMTASKSCOMPLETED: " + before + " -> 999. "
                + "Leave and re-enter the Sponsor Office screen to see it refresh.");
            return "Sponsor tier tasks " + before + "->999";
        }

        public static void ResetLevelProgress()
        {
            try
            {
                MissionsManager mm = UnityEngine.Object.FindObjectOfType<MissionsManager>();
                if ((object)mm == null)
                {
                    ModLog.Warn("[CareerReset] ResetLevelProgress: MissionsManager not found in scene.");
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
                    ModLog.Warn("[CareerReset] ResetLevelProgress: could not enumerate mission groups - "
                        + "live IsComplete flags were not touched, only MissionsManager.Reload() ran.");
                }

                try
                {
                    mm.Reload();
                    ModLog.Debug("[CareerReset] MissionsManager.Reload() called - internal completion/claim lists cleared.");
                }
                catch (Exception ex)
                {
                    ModLog.Warn("[CareerReset] mm.Reload() threw: " + ex.Message);
                }

                ModLog.Debug("[CareerReset] Reset Level Progress: cleared IsComplete on "
                    + clearedCount + "/" + totalMissions + " mission(s). Group/tour lock state is derived "
                    + "from mission completion, so it resets as a side effect of this.");
                ModLog.Debug("[CareerReset] NOTE: this resets the live session state immediately. "
                    + "If completed missions reappear after a full game restart, the persisted PrefsManager "
                    + "blob (key \"MissionsData\") needs clearing too - flag it and we'll add that next.");
                LastResult = "Cleared " + clearedCount + " mission(s)";
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[CareerReset] ResetLevelProgress: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "CareerReset");
                LastResult = "Error - see log";
            }
        }

        public static void ResetSponsorProgress()
        {
            try
            {
                GameData gd = UnityEngine.Object.FindObjectOfType<GameData>();
                if ((object)gd == null)
                {
                    ModLog.Warn("[CareerReset] ResetSponsorProgress: GameData not found in scene.");
                    LastResult = "GameData not found";
                    return;
                }

                cyFLnlM before = gd.GetTeamDivision();
                gd.SetTeamDivision(cyFLnlM.Novice);
                bool divisionChanged = before != cyFLnlM.Novice;
                ModLog.Feedback("[CareerReset] Sponsor division: " + before + " -> Novice "
                    + "(persisted via PrefsManager key \"SPONSORDIVISION\").");

                object prefsInstance = FindSingletonInstance(typeof(PrefsManager));
                PrefsManager prefs = prefsInstance as PrefsManager;
                int beforeRep = -1;
                bool prefsOk = false;
                int sponsorNodesCleared = 0;
                if ((object)prefs != null)
                {
                    beforeRep = prefs.GetInt("TOTALREP");
                    prefs.SetInt("TOTALREP", 0);

                    for (int i = 1; i <= 3; i++)
                    {
                        string key = "SPONSOR_" + i;
                        if (prefs.GetInt(key) != 0)
                        {
                            prefs.SetInt(key, 0);
                            sponsorNodesCleared++;
                        }
                    }

                    int beforeTeamTasks = prefs.GetInt("TEAMTASKSCOMPLETED");
                    if (beforeTeamTasks != 0)
                    {
                        prefs.SetInt("TEAMTASKSCOMPLETED", 0);
                        sponsorNodesCleared++;
                    }

                    try { prefs.Save(); prefsOk = true; }
                    catch (Exception exSave) { ModLog.Warn("[CareerReset] prefs.Save() threw: " + exSave.Message); }
                    ModLog.Feedback("[CareerReset] TOTALREP: " + beforeRep + " -> 0. TEAMTASKSCOMPLETED: " + beforeTeamTasks
                        + " -> 0. SPONSOR_1/2/3 cleared: " + sponsorNodesCleared + ".");
                }
                else
                {
                    ModLog.Warn("[CareerReset] Could not resolve a live PrefsManager instance - "
                        + "TOTALREP was not touched. Division reset above still applies.");
                }

                ModLog.Debug("[CareerReset] Reset Sponsor Progress complete.");
                if (!divisionChanged && sponsorNodesCleared == 0 && (beforeRep == 0 || !prefsOk))
                    LastResult = "Already at Novice / 0 rep / 0 sponsor nodes - nothing to reset";
                else
                    LastResult = "Division " + before + "->Novice, rep " + (beforeRep >= 0 ? beforeRep.ToString() : "?")
                        + "->0, " + sponsorNodesCleared + " sponsor node(s) cleared";
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[CareerReset] ResetSponsorProgress: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "CareerReset");
                LastResult = "Error - see log";
            }
        }

        // ── Adjust Rep (+/-) ─────────────────────────────────────────────
        public static int CurrentRep
        {
            get
            {
                object prefsInstance = FindSingletonInstance(typeof(PrefsManager));
                PrefsManager prefs = prefsInstance as PrefsManager;
                if ((object)prefs == null) return 0;
                try { return prefs.GetInt("TOTALREP"); }
                catch (Exception ex) { MelonLogger.Error("[CareerReset] CurrentRep get: " + ex.Message); Telemetry.ReportErrorAsync(ex, "CareerReset"); return 0; }
            }
        }

        private const int RepBaseStep = 1000;
        private const int RepMultiplierSoftCap = 10;
        private const int RepMultiplierHardCap = 200;
        private const int RepMultiplierCoarseStep = 10;

        public static int RepMultiplierLevel { get; private set; } = 1;
        public static int InGameRepMultiplierLevel { get; private set; } = 1;
        public static int RepStepAmount { get { return RepBaseStep * RepMultiplierLevel; } }
        public static int InGameRepStepAmount { get { return RepBaseStep * InGameRepMultiplierLevel; } }

        private static int BumpRepMultiplier(int level, int dir)
        {
            if (dir > 0)
            {
                if (level < RepMultiplierSoftCap) return level + 1;
                if (level < RepMultiplierHardCap)
                    return Math.Min(RepMultiplierHardCap, level + RepMultiplierCoarseStep);
                return level;
            }
            if (level > RepMultiplierSoftCap)
                return Math.Max(RepMultiplierSoftCap, level - RepMultiplierCoarseStep);
            if (level > 1) return level - 1;
            return level;
        }

        public static void IncreaseRepMultiplier() { RepMultiplierLevel = BumpRepMultiplier(RepMultiplierLevel, 1); }
        public static void DecreaseRepMultiplier() { RepMultiplierLevel = BumpRepMultiplier(RepMultiplierLevel, -1); }
        public static void IncreaseInGameRepMultiplier() { InGameRepMultiplierLevel = BumpRepMultiplier(InGameRepMultiplierLevel, 1); }
        public static void DecreaseInGameRepMultiplier() { InGameRepMultiplierLevel = BumpRepMultiplier(InGameRepMultiplierLevel, -1); }

        /// <summary>Set total/lifetime rep to an exact value, typed directly rather than stepped with +/-.
        /// Internally computed as a delta against the current live value and routed through AdjustRep
        /// so it goes through the same clamp/save/feedback/live-HUD-sync path as the +/- buttons.</summary>
        public static void SetRep(int newValue)
        {
            if (newValue < 0) newValue = 0;
            AdjustRep(newValue - LiveRepValue);
        }

        /// <summary>Set this session's in-game rep to an exact value, typed directly rather than stepped.
        /// Same delta-through-AdjustInGameRep approach as SetRep. Returns false if not in a session.</summary>
        public static bool SetInGameRep(int newValue)
        {
            if (newValue < 0) newValue = 0;
            return AdjustInGameRep(newValue - CurrentInGameRep);
        }

        public static void AdjustRep(int amount)
        {
            try
            {
                object prefsInstance = FindSingletonInstance(typeof(PrefsManager));
                PrefsManager prefs = prefsInstance as PrefsManager;
                if ((object)prefs == null)
                {
                    ModLog.Warn("[CareerReset] AdjustRep: Could not resolve a PrefsManager instance.");
                    LastResult = "PrefsManager not found";
                    return;
                }

                int before = prefs.GetInt("TOTALREP");
                int after = before + amount;
                if (after < 0) after = 0;
                prefs.SetInt("TOTALREP", after);
                try { prefs.Save(); }
                catch (Exception exSave) { ModLog.Warn("[CareerReset] AdjustRep prefs.Save() threw: " + exSave.Message); }

                ModLog.Feedback("[CareerReset] TOTALREP: " + before + " -> " + after + " (" + (amount >= 0 ? "+" : "") + amount + ")");

                bool liveOk = AdjustLiveRep(amount);

                LastResult = "Rep " + before + " -> " + after + (liveOk ? " (+ live HUD updated)" : " (live HUD field not found - see log)");
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[CareerReset] AdjustRep: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "CareerReset");
                LastResult = "Error - see log";
            }
        }

        private static PropertyInfo _backendRepProp;
        private static bool _backendRepPropSearched;

        private static PropertyInfo GetBackendRepProp()
        {
            if (!_backendRepPropSearched)
            {
                _backendRepPropSearched = true;
                _backendRepProp = typeof(DevCommandsBackEnd).GetProperty("M\u0083\u007B\u007E\u005DEe",
                    BindingFlags.Public | BindingFlags.Static);
                if ((object)_backendRepProp == null)
                    ModLog.Warn("[CareerReset] LiveRepValue: DevCommandsBackEnd.M{~]Ee property not found.");
            }
            return _backendRepProp;
        }

        public static int LiveRepValue
        {
            get
            {
                try
                {
                    PropertyInfo p = GetBackendRepProp();
                    if ((object)p == null) return 0;
                    return (int)p.GetValue(null, null);
                }
                catch (Exception ex) { MelonLogger.Error("[CareerReset] LiveRepValue get: " + ex.Message); Telemetry.ReportErrorAsync(ex, "CareerReset"); return 0; }
            }
        }

        private static bool AdjustLiveRep(int amount)
        {
            try
            {
                PropertyInfo p = GetBackendRepProp();
                if ((object)p == null)
                {
                    ModLog.Warn("[CareerReset] AdjustLiveRep: backend rep property not found.");
                    return false;
                }

                int before = (int)p.GetValue(null, null);
                int after = before + amount;
                if (after < 0) after = 0;
                p.SetValue(null, after, null);

                ModLog.Debug("[CareerReset] Live rep (backend, Steam-submitted as \"reputation_s2_\"): "
                    + before + " -> " + after);
                return true;
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[CareerReset] AdjustLiveRep: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "CareerReset");
                return false;
            }
        }

        public static int CurrentInGameRep
        {
            get
            {
                try
                {
                    PlayerInfoImpact pii = FindLocalPlayerInfoImpact();
                    if ((object)pii == null) return 0;
                    ObscuredInt oi;
                    if (!TryGetInGameRepField(pii, out oi)) return 0;
                    return DecodeObscuredInt(oi);
                }
                catch (Exception ex) { MelonLogger.Error("[CareerReset] CurrentInGameRep get: " + ex.Message); Telemetry.ReportErrorAsync(ex, "CareerReset"); return 0; }
            }
        }

        public static bool AdjustInGameRep(int amount)
        {
            try
            {
                PlayerInfoImpact pii = FindLocalPlayerInfoImpact();
                if ((object)pii == null)
                {
                    ModLog.Warn("[CareerReset] AdjustInGameRep: local PlayerInfoImpact not found (not in a session?).");
                    LastResult = "Not in a session";
                    return false;
                }

                FieldInfo statsField = typeof(PlayerInfoImpact).GetField("d\u0082kxXXv",
                    BindingFlags.Public | BindingFlags.Instance);
                if ((object)statsField == null)
                {
                    ModLog.Warn("[CareerReset] AdjustInGameRep: stats sub-object field (d]kxXXv) not found on PlayerInfoImpact.");
                    LastResult = "Field not found";
                    return false;
                }

                object statsObj = statsField.GetValue(pii);
                if (statsObj == null)
                {
                    ModLog.Warn("[CareerReset] AdjustInGameRep: stats sub-object was null on this instance.");
                    LastResult = "Stats object null";
                    return false;
                }

                FieldInfo repField = statsObj.GetType().GetField("LgqK\u005DLp",
                    BindingFlags.Public | BindingFlags.Instance);
                if ((object)repField == null)
                {
                    ModLog.Warn("[CareerReset] AdjustInGameRep: LgqK]Lp field not found on stats sub-object.");
                    LastResult = "Field not found";
                    return false;
                }

                ObscuredInt beforeOi = (ObscuredInt)repField.GetValue(statsObj);
                int beforeInt = DecodeObscuredInt(beforeOi);
                int afterInt = beforeInt + amount;
                if (afterInt < 0) afterInt = 0;
                ObscuredInt afterOi = EncodeObscuredInt(afterInt);
                repField.SetValue(statsObj, afterOi);

                ModLog.Feedback("[CareerReset] In-game rep (LgqK]Lp): " + beforeInt + " -> " + afterInt);
                LastResult = "In-Game Rep " + beforeInt + " -> " + afterInt;
                return true;
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[CareerReset] AdjustInGameRep: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "CareerReset");
                LastResult = "Error - see log";
                return false;
            }
        }

        private static bool TryGetInGameRepField(PlayerInfoImpact pii, out ObscuredInt value)
        {
            value = default(ObscuredInt);
            FieldInfo statsField = typeof(PlayerInfoImpact).GetField("d\u0082kxXXv",
                BindingFlags.Public | BindingFlags.Instance);
            if ((object)statsField == null) return false;
            object statsObj = statsField.GetValue(pii);
            if (statsObj == null) return false;
            FieldInfo repField = statsObj.GetType().GetField("LgqK\u005DLp",
                BindingFlags.Public | BindingFlags.Instance);
            if ((object)repField == null) return false;
            value = (ObscuredInt)repField.GetValue(statsObj);
            return true;
        }

        private static int DecodeObscuredInt(ObscuredInt oi)
        {
            MethodInfo m = FindDZlraRfOverload(new Type[] { typeof(ObscuredInt) }, typeof(int));
            if ((object)m == null) throw new Exception("ObscuredInt decode method (DZlraRf(ObscuredInt) -> int) not found.");
            return (int)m.Invoke(null, new object[] { oi });
        }

        private static ObscuredInt EncodeObscuredInt(int value)
        {
            MethodInfo m = FindDZlraRfOverload(new Type[] { typeof(int) }, typeof(ObscuredInt));
            if ((object)m == null) throw new Exception("ObscuredInt encode method (DZlraRf(int) -> ObscuredInt) not found.");
            return (ObscuredInt)m.Invoke(null, new object[] { value });
        }

        private static MethodInfo FindDZlraRfOverload(Type[] paramTypes, Type returnType)
        {
            MethodInfo[] candidates = typeof(ObscuredInt).GetMethods(BindingFlags.Public | BindingFlags.Static);
            for (int i = 0; i < candidates.Length; i++)
            {
                MethodInfo m = candidates[i];
                if (!string.Equals(m.Name, "DZlraRf", StringComparison.Ordinal)) continue;
                if (!m.ReturnType.Equals(returnType)) continue;
                ParameterInfo[] ps = m.GetParameters();
                if (ps.Length != paramTypes.Length) continue;
                bool match = true;
                for (int j = 0; j < ps.Length; j++)
                    if (!ps[j].ParameterType.Equals(paramTypes[j])) { match = false; break; }
                if (match) return m;
            }
            return null;
        }

        private static PlayerInfoImpact FindLocalPlayerInfoImpact()
        {
            PlayerInfoImpact[] all = UnityEngine.Object.FindObjectsOfType<PlayerInfoImpact>();
            MethodInfo isHuman = typeof(PlayerInfoImpact).GetMethod("IsHumanControlled",
                BindingFlags.Public | BindingFlags.Instance);
            if ((object)isHuman == null) return all.Length > 0 ? all[0] : null;
            for (int i = 0; i < all.Length; i++)
            {
                try { if ((bool)isHuman.Invoke(all[i], null)) return all[i]; }
                catch { }
            }
            return null;
        }

        public static bool UnlockAllEnabled
        {
            get
            {
                try
                {
                    CustomizationManager cm = UnityEngine.Object.FindObjectOfType<CustomizationManager>();
                    if ((object)cm == null) return false;
                    return cm.mZVyMyX;
                }
                catch (Exception ex)
                {
                    MelonLogger.Error("[CareerReset] UnlockAllEnabled get: " + ex.Message);
                    Telemetry.ReportErrorAsync(ex, "CareerReset");
                    return false;
                }
            }
        }

        public static void UnlockAllOn()
        {
            if (!UnlockAllEnabled) ToggleUnlockAll();
        }

        public static void UnlockAllOff()
        {
            if (UnlockAllEnabled) ToggleUnlockAll();
        }

        public static void ToggleUnlockAll()
        {
            try
            {
                CustomizationManager cm = UnityEngine.Object.FindObjectOfType<CustomizationManager>();
                if ((object)cm == null)
                {
                    ModLog.Warn("[CareerReset] ToggleUnlockAll: CustomizationManager not found in scene.");
                    LastResult = "CustomizationManager not found";
                    return;
                }

                bool newVal = !cm.mZVyMyX;
                cm.mZVyMyX = newVal;

                object prefsInstance = FindSingletonInstance(typeof(PrefsManager));
                PrefsManager prefs = prefsInstance as PrefsManager;
                if ((object)prefs != null)
                {
                    try { prefs.Save(); }
                    catch (Exception exSave) { ModLog.Warn("[CareerReset] ToggleUnlockAll prefs.Save() threw: " + exSave.Message); }
                }

                ModLog.Feedback("[CareerReset] Unlock All -> " + (newVal ? "ON" : "OFF"));
                ModLog.Debug("[CareerReset] Unlock All verify readback: " + cm.mZVyMyX
                    + " | NOTE: shed/customization grid may cache lock icons — re-enter that screen if needed.");
                LastResult = "Unlock All " + (newVal ? "ON - all bikes/gear unlocked" : "OFF");
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[CareerReset] ToggleUnlockAll: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "CareerReset");
                LastResult = "Error - see log";
            }
        }

        public static string DumpBikeUnlockStatus()
        {
            try
            {
                CustomizationManager cm = UnityEngine.Object.FindObjectOfType<CustomizationManager>();
                if ((object)cm == null)
                {
                    ModLog.Warn("[CareerReset] DumpBikeUnlockStatus: CustomizationManager not found in scene.");
                    return "CustomizationManager not found";
                }

                CustomizationItem[] allItems = Resources.FindObjectsOfTypeAll<CustomizationItem>();
                ModLog.Debug("[CareerReset] === Bike Unlock Status Dump ===");
                ModLog.Debug("[CareerReset] UnlockAllEnabled (mZVyMyX) = " + cm.mZVyMyX
                    + " | " + allItems.Length + " total CustomizationItem asset(s) loaded");

                int shown = 0;
                for (int i = 0; i < allItems.Length; i++)
                {
                    CustomizationItem item = allItems[i];
                    if ((object)item == null) continue;
                    string slotName = item.slot.ToString();
                    if (!string.Equals(slotName, "Bike", StringComparison.Ordinal)
                        && !string.Equals(slotName, "BikeType", StringComparison.Ordinal))
                        continue;

                    bool unlocked = false;
                    try { unlocked = cm.IsItemUnlocked(item); }
                    catch (Exception exItem) { ModLog.Warn("[CareerReset]   IsItemUnlocked threw for \"" + item.displayName + "\": " + exItem.Message); }

                    ModLog.Debug("[CareerReset]   itemID=" + item.itemID
                        + " name=\"" + item.displayName + "\""
                        + " slot=" + slotName
                        + " rarity=" + item.rarity
                        + " unlocked=" + unlocked);
                    shown++;
                }

                ModLog.Debug("[CareerReset] === End dump: " + shown + " bike-slot item(s) ===");
                LastResult = "Logged " + shown + " bike item(s) - check MelonLoader log";
                return LastResult;
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[CareerReset] DumpBikeUnlockStatus: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "CareerReset");
                LastResult = "Error - see log";
                return LastResult;
            }
        }

        // ── Switch Sponsor ────────────────────────────────────────────────
        private static PropertyInfo _currentTeamProp;
        private static bool _currentTeamPropSearched;

        private static PropertyInfo GetCurrentTeamProp()
        {
            if (!_currentTeamPropSearched)
            {
                _currentTeamPropSearched = true;
                _currentTeamProp = typeof(DevCommandsBackEnd).GetProperty("lno\u0082zMq",
                    BindingFlags.Public | BindingFlags.Static);
                if ((object)_currentTeamProp == null)
                    ModLog.Warn("[CareerReset] Switch Sponsor: DevCommandsBackEnd.lno]zMq property not found.");
            }
            return _currentTeamProp;
        }

        private static TeamInfo[] GetAllTeams()
        {
            GameData gd = UnityEngine.Object.FindObjectOfType<GameData>();
            if ((object)gd == null) return new TeamInfo[0];

            FieldInfo teamsField = typeof(GameData).GetField("D\u0083nWNgg", BindingFlags.Public | BindingFlags.Instance);
            if ((object)teamsField == null)
            {
                ModLog.Warn("[CareerReset] Switch Sponsor: team list field (D]nWNgg) not found on GameData.");
                return new TeamInfo[0];
            }

            return teamsField.GetValue(gd) as TeamInfo[] ?? new TeamInfo[0];
        }

        private static int GetCurrentTeamId()
        {
            PropertyInfo p = GetCurrentTeamProp();
            if ((object)p == null) return 0;
            try { return (int)p.GetValue(null, null); }
            catch (Exception ex) { MelonLogger.Error("[CareerReset] GetCurrentTeamId: " + ex.Message); Telemetry.ReportErrorAsync(ex, "CareerReset"); return 0; }
        }

        private static void SetCurrentTeamId(int id)
        {
            PropertyInfo p = GetCurrentTeamProp();
            if ((object)p == null) return;
            try { p.SetValue(null, id, null); }
            catch (Exception ex) { MelonLogger.Error("[CareerReset] SetCurrentTeamId: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "CareerReset"); }
        }

        public static string CurrentSponsorName
        {
            get
            {
                try
                {
                    int currentId = GetCurrentTeamId();
                    if (currentId == 0) return "None (Unsponsored)";

                    TeamInfo[] teams = GetAllTeams();
                    for (int i = 0; i < teams.Length; i++)
                        if ((object)teams[i] != null && teams[i].teamID == currentId)
                            return teams[i].name;

                    return "Unknown (id " + currentId + ")";
                }
                catch (Exception ex) { MelonLogger.Error("[CareerReset] CurrentSponsorName: " + ex.Message); Telemetry.ReportErrorAsync(ex, "CareerReset"); return "Error"; }
            }
        }

        public static void NextSponsor() { StepSponsor(1); }
        public static void PreviousSponsor() { StepSponsor(-1); }

        private static void StepSponsor(int direction)
        {
            try
            {
                TeamInfo[] teams = GetAllTeams();
                if (teams.Length == 0)
                {
                    ModLog.Warn("[CareerReset] StepSponsor: no teams found - GameData not in scene yet?");
                    LastResult = "No teams found";
                    return;
                }

                int currentId = GetCurrentTeamId();
                int idx = 0;
                for (int i = 0; i < teams.Length; i++)
                    if ((object)teams[i] != null && teams[i].teamID == currentId) { idx = i; break; }

                idx = ((idx + direction) % teams.Length + teams.Length) % teams.Length;
                TeamInfo next = teams[idx];
                if ((object)next == null)
                {
                    LastResult = "Team slot was null";
                    return;
                }

                SetCurrentTeamId(next.teamID);
                ModLog.Feedback("[CareerReset] Sponsor: " + currentId + " -> " + next.teamID + " (\"" + next.name + "\")");
                LastResult = "Sponsor: " + next.name;
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[CareerReset] StepSponsor: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "CareerReset");
                LastResult = "Error - see log";
            }
        }

        // ══════════════════════════════════════════════════════════════
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
                Telemetry.ReportErrorAsync(ex, "CareerReset");
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
                    ModLog.Warn("[CareerReset] No field of type " + fieldType.Name
                        + " found on " + owner.Name + " (flags=" + flags + "). Dumping candidate fields for manual ID:");
                    for (int i = 0; i < fields.Length; i++)
                        ModLog.Debug("    " + fields[i].FieldType.Name + "  " + fields[i].Name);
                    return null;
                }
                if (matchCount > 1)
                    ModLog.Warn("[CareerReset] " + matchCount + " fields of type " + fieldType.Name
                        + " found on " + owner.Name + " - using the first one: " + match.Name);
                else
                    ModLog.Debug("[CareerReset] Found field '" + match.Name + "' (" + fieldType.Name
                        + ") on " + owner.Name + ".");

                return match;
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[CareerReset] FindFieldByType(" + owner.Name + ", " + fieldType.Name + "): " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "CareerReset");
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
                        ModLog.Debug("[CareerReset] Found " + t.Name + " instance via FindObjectOfType.");
                        return found;
                    }
                    ModLog.Warn("[CareerReset] FindObjectOfType(" + t.Name + ") returned null, trying static member scan...");
                }

                PropertyInfo[] props = t.GetProperties(BindingFlags.Public | BindingFlags.Static);
                for (int i = 0; i < props.Length; i++)
                {
                    if (t.IsAssignableFrom(props[i].PropertyType))
                    {
                        object val = props[i].GetValue(null, null);
                        if ((object)val != null)
                        {
                            ModLog.Debug("[CareerReset] Found " + t.Name + " instance via static property '" + props[i].Name + "'.");
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
                            ModLog.Debug("[CareerReset] Found " + t.Name + " instance via static field '" + fields[i].Name + "'.");
                            return val;
                        }
                    }
                }

                ModLog.Warn("[CareerReset] Could not resolve a singleton instance for " + t.Name + ".");
                return null;
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[CareerReset] FindSingletonInstance(" + t.Name + "): " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "CareerReset");
                return null;
            }
        }

        private static string SafeId(MissionData md)
        {
            try { return md.Id; } catch { return "?"; }
        }
    }
}

