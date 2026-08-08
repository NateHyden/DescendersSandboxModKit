using MelonLoader;
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
                    ModLog.Feedback("[CareerReset] (sponsor interest) " + key + ": " + before + " -> 3.");
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

            ModLog.Feedback("[CareerReset] TEAMTASKSCOMPLETED: " + before + " -> 999. "
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
                ModLog.Feedback("[CareerReset] Sponsor division: " + before + " -> Novice "
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
                    ModLog.Feedback("[CareerReset] TOTALREP: " + beforeRep + " -> 0. TEAMTASKSCOMPLETED: " + beforeTeamTasks
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

        // ── Adjust Rep (+/-) ─────────────────────────────────────────────
        // Same "TOTALREP" PrefsManager key TryCompleteViaProgressNodes/ResetSponsorProgress
        // already use - this just nudges it instead of setting an absolute target.
        public static int CurrentRep
        {
            get
            {
                object prefsInstance = FindSingletonInstance(typeof(PrefsManager));
                PrefsManager prefs = prefsInstance as PrefsManager;
                if ((object)prefs == null) return 0;
                try { return prefs.GetInt("TOTALREP"); }
                catch (Exception ex) { MelonLogger.Error("[CareerReset] CurrentRep get: " + ex.Message); return 0; }
            }
        }

        // ── Rep step multiplier (x1-x10, independent per row) ────────────
        private const int RepBaseStep = 1000;
        public static int RepMultiplierLevel { get; private set; } = 1;
        public static int InGameRepMultiplierLevel { get; private set; } = 1;
        public static int RepStepAmount { get { return RepBaseStep * RepMultiplierLevel; } }
        public static int InGameRepStepAmount { get { return RepBaseStep * InGameRepMultiplierLevel; } }

        public static void IncreaseRepMultiplier() { if (RepMultiplierLevel < 10) RepMultiplierLevel++; }
        public static void DecreaseRepMultiplier() { if (RepMultiplierLevel > 1) RepMultiplierLevel--; }
        public static void IncreaseInGameRepMultiplier() { if (InGameRepMultiplierLevel < 10) InGameRepMultiplierLevel++; }
        public static void DecreaseInGameRepMultiplier() { if (InGameRepMultiplierLevel > 1) InGameRepMultiplierLevel--; }

        public static void AdjustRep(int amount)
        {
            try
            {
                object prefsInstance = FindSingletonInstance(typeof(PrefsManager));
                PrefsManager prefs = prefsInstance as PrefsManager;
                if ((object)prefs == null)
                {
                    MelonLogger.Warning("[CareerReset] AdjustRep: Could not resolve a PrefsManager instance.");
                    LastResult = "PrefsManager not found";
                    return;
                }

                int before = prefs.GetInt("TOTALREP");
                int after = before + amount;
                if (after < 0) after = 0;
                prefs.SetInt("TOTALREP", after);
                try { prefs.Save(); }
                catch (Exception exSave) { MelonLogger.Warning("[CareerReset] AdjustRep prefs.Save() threw: " + exSave.Message); }

                ModLog.Feedback("[CareerReset] TOTALREP: " + before + " -> " + after + " (" + (amount >= 0 ? "+" : "") + amount + ")");

                // TOTALREP only drives sponsor-tier gating - it is NOT the value shown
                // in the on-screen HUD counter (confirmed 2026-08-04: TOTALREP sits in
                // the low thousands, the HUD counter runs 1,000,000+, a scale mismatch
                // that can't be the same field). AdjustLiveRep hits the actual live
                // field the HUD reads.
                bool liveOk = AdjustLiveRep(amount);

                LastResult = "Rep " + before + " -> " + after + (liveOk ? " (+ live HUD updated)" : " (live HUD field not found - see log)");
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[CareerReset] AdjustRep: " + ex.Message);
                LastResult = "Error - see log";
            }
        }

        // ── Live On-Screen Rep Counter ───────────────────────────────────
        // CORRECTED 2026-08-04 (previous field was wrong - see below).
        //
        // The bottom-of-screen "R" HUD value is DevCommandsBackEnd.M{~]Ee, a clean
        // static int wrapper (same pattern/class as the sponsor-switching field) over
        // DescendersBackEnd.M{~]Ee, an abstract ObscuredInt property on the platform
        // backend singleton. Confirmed directly in the decompile: this exact field is
        // what DescendersBackEndSteam submits to Steam's leaderboard as
        // "reputation_s2_<teamID>" - i.e. it's the actual persistent reputation
        // counter, not a per-session score.
        //
        // What was wrong before: PlayerInfoImpact.d]kxXXv.LgqK]Lp. That field is real
        // and does accumulate on trick combos, but it lives on a stats sub-object that
        // gets freshly reconstructed each session (gated on SessionStarted()) -
        // confirmed live, it read back as 0 immediately after being written to. It's
        // a session-local combo-score counter, not the lifetime rep total.
        //
        // PERSISTENCE CAVEAT: this in-memory value is what SubmitToLeaderboard/SetStat
        // send to Steam - but only when the game's own sync code actually calls those
        // (end of run/session, not continuously). Changing this field updates what
        // will be submitted next time that fires; it doesn't push to Steam's servers
        // immediately by itself. If the number doesn't survive a full session/lobby
        // restart, that sync timing is the next thing to check - would need a fresh
        // scene dump captured right after a submission point to confirm.
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
                    MelonLogger.Warning("[CareerReset] LiveRepValue: DevCommandsBackEnd.M{~]Ee property not found.");
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
                catch (Exception ex) { MelonLogger.Error("[CareerReset] LiveRepValue get: " + ex.Message); return 0; }
            }
        }

        private static bool AdjustLiveRep(int amount)
        {
            try
            {
                PropertyInfo p = GetBackendRepProp();
                if ((object)p == null)
                {
                    MelonLogger.Warning("[CareerReset] AdjustLiveRep: backend rep property not found.");
                    return false;
                }

                int before = (int)p.GetValue(null, null);
                int after = before + amount;
                if (after < 0) after = 0;
                p.SetValue(null, after, null);

                MelonLogger.Msg("[CareerReset] Live rep (backend, Steam-submitted as \"reputation_s2_\"): "
                    + before + " -> " + after);
                return true;
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[CareerReset] AdjustLiveRep: " + ex.Message);
                return false;
            }
        }

        // ── In-Game Rep (session-local combo score) ──────────────────────
        // This is the OTHER field from the original investigation:
        // PlayerInfoImpact.d]kxXXv.LgqK]Lp - a CodeStage ObscuredInt on a per-player
        // stats sub-object that accumulates every time a trick combo scores. It's
        // NOT the persistent "Total Rep" (that's DevCommandsBackEnd.M{~]Ee, above) -
        // this one gets freshly reconstructed to 0 each session (gated on
        // SessionStarted()), confirmed live. That makes it exactly the "current
        // in-game session" counter, as opposed to the lifetime total.
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
                catch (Exception ex) { MelonLogger.Error("[CareerReset] CurrentInGameRep get: " + ex.Message); return 0; }
            }
        }

        public static bool AdjustInGameRep(int amount)
        {
            try
            {
                PlayerInfoImpact pii = FindLocalPlayerInfoImpact();
                if ((object)pii == null)
                {
                    MelonLogger.Warning("[CareerReset] AdjustInGameRep: local PlayerInfoImpact not found (not in a session?).");
                    LastResult = "Not in a session";
                    return false;
                }

                FieldInfo statsField = typeof(PlayerInfoImpact).GetField("d\u0082kxXXv",
                    BindingFlags.Public | BindingFlags.Instance);
                if ((object)statsField == null)
                {
                    MelonLogger.Warning("[CareerReset] AdjustInGameRep: stats sub-object field (d]kxXXv) not found on PlayerInfoImpact.");
                    LastResult = "Field not found";
                    return false;
                }

                object statsObj = statsField.GetValue(pii);
                if (statsObj == null)
                {
                    MelonLogger.Warning("[CareerReset] AdjustInGameRep: stats sub-object was null on this instance.");
                    LastResult = "Stats object null";
                    return false;
                }

                FieldInfo repField = statsObj.GetType().GetField("LgqK\u005DLp",
                    BindingFlags.Public | BindingFlags.Instance);
                if ((object)repField == null)
                {
                    MelonLogger.Warning("[CareerReset] AdjustInGameRep: LgqK]Lp field not found on stats sub-object.");
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

        // ObscuredInt's own conversion operators were renamed along with everything
        // else in this obfuscated build (the [SpecialName] "op_Implicit" methods got
        // scrambled to DZlraRf), so the implicit int<->ObscuredInt conversions the
        // type is normally used with don't work from source compiled against this
        // DLL - both overloads have to be invoked explicitly via reflection.
        //
        // CRITICAL: DZlraRf(ObscuredInt) is not unique - there are THREE overloads
        // that all take a single ObscuredInt parameter and differ only by RETURN
        // TYPE (int, ObscuredFloat, ObscuredDouble - the obfuscator collapsed what
        // used to be differently-named conversion methods onto the same string).
        // GetMethod(name, flags, binder, parameterTypes, modifiers) matches on
        // parameter types only, so it can silently return the WRONG overload here -
        // confirmed live: it picked one of the float/double versions, and casting
        // that boxed result to int threw "Cannot cast from source type to
        // destination type". Have to walk GetMethods() and check ReturnType too.
        // (Not currently used by LiveRepValue/AdjustLiveRep anymore - kept in case a
        // future field needs the same ObscuredInt encode/decode dance.)
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
            // NOTE: uses .Equals(), never == or != on Type objects - Type's operator
            // overloads compile to calls to Type.op_Equality/op_Inequality, and this
            // build's mscorlib.dll is missing those (a documented, recurring gotcha
            // in this project - "Type::op_Equality spam" in How_to_fix_after_update.md).
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

        // ── Unlock All (bikes + gear) ─────────────────────────────────────
        // CustomizationManager.IsItemUnlocked() reads, verbatim from the post-update
        // decompile:
        //     return this.<unlockedList>.Contains(item) || this.mZVyMyX;
        // mZVyMyX is a public bool property (plain-ASCII obfuscated name - no escapes
        // needed, safe to reference directly, NOT a runtime-subclass situation) backed
        // by PrefsManager key "UnlockAll". Flipping it true makes every
        // CustomizationItem report as unlocked immediately. Bikes are covered by the
        // same flag - the slot enum (mFWXh}~) includes Bike and BikeType as
        // customization slots, so this is one flag for both asks, not two separate
        // systems. Confirmed directly in the decompile, 2026-08-03.
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
                    return false;
                }
            }
        }

        // Explicit on/off - simpler UI than a single toggle button with a
        // confirm-arm dance. If already in the requested state, no-ops.
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
                    MelonLogger.Warning("[CareerReset] ToggleUnlockAll: CustomizationManager not found in scene.");
                    LastResult = "CustomizationManager not found";
                    return;
                }

                bool newVal = !cm.mZVyMyX;
                cm.mZVyMyX = newVal;

                // The property setter writes straight to PrefsManager but doesn't call
                // Save() itself - do that explicitly here so the flag survives an app
                // restart, matching every other PrefsManager write in this file.
                object prefsInstance = FindSingletonInstance(typeof(PrefsManager));
                PrefsManager prefs = prefsInstance as PrefsManager;
                if ((object)prefs != null)
                {
                    try { prefs.Save(); }
                    catch (Exception exSave) { MelonLogger.Warning("[CareerReset] ToggleUnlockAll prefs.Save() threw: " + exSave.Message); }
                }

                MelonLogger.Msg("[CareerReset] Unlock All (bikes + gear): " + newVal
                    + " | verify readback: " + cm.mZVyMyX
                    + " | NOTE: the shed/customization grid may cache lock icons at build time - "
                    + "if items still show locked, leave and re-enter that screen to force a rebuild.");
                LastResult = "Unlock All " + (newVal ? "ON - all bikes/gear unlocked" : "OFF");
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[CareerReset] ToggleUnlockAll: " + ex.Message);
                LastResult = "Error - see log";
            }
        }

        // ── Diagnostic: Dump Bike Unlock Status ─────────────────────────
        // Asks the game's own IsItemUnlocked(CustomizationItem) directly for every
        // Bike/BikeType-slot item, instead of guessing from the (unreliable - this
        // build's string literals are shuffled/decoy) decompiled source. Definitive
        // ground truth for whether the UnlockAll flag is actually reaching these
        // specific items or whether something else is gating them (team/sponsor
        // ownership, DLC, a separate cached check, etc).
        public static string DumpBikeUnlockStatus()
        {
            try
            {
                CustomizationManager cm = UnityEngine.Object.FindObjectOfType<CustomizationManager>();
                if ((object)cm == null)
                {
                    MelonLogger.Warning("[CareerReset] DumpBikeUnlockStatus: CustomizationManager not found in scene.");
                    return "CustomizationManager not found";
                }

                CustomizationItem[] allItems = Resources.FindObjectsOfTypeAll<CustomizationItem>();
                MelonLogger.Msg("[CareerReset] === Bike Unlock Status Dump ===");
                MelonLogger.Msg("[CareerReset] UnlockAllEnabled (mZVyMyX) = " + cm.mZVyMyX
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
                    catch (Exception exItem) { MelonLogger.Warning("[CareerReset]   IsItemUnlocked threw for \"" + item.displayName + "\": " + exItem.Message); }

                    MelonLogger.Msg("[CareerReset]   itemID=" + item.itemID
                        + " name=\"" + item.displayName + "\""
                        + " slot=" + slotName
                        + " rarity=" + item.rarity
                        + " unlocked=" + unlocked);
                    shown++;
                }

                MelonLogger.Msg("[CareerReset] === End dump: " + shown + " bike-slot item(s) ===");
                LastResult = "Logged " + shown + " bike item(s) - check MelonLoader log";
                return LastResult;
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[CareerReset] DumpBikeUnlockStatus: " + ex.Message);
                LastResult = "Error - see log";
                return LastResult;
            }
        }

        // ── Switch Sponsor ────────────────────────────────────────────────
        // Confirmed directly in the decompile, 2026-08-04: there's a whole dedicated
        // game state for this (State_TeamSelect.cs) that writes the exact same field
        // this drives. The actual "which sponsor am I currently signed to" value is
        // DescendersBackEnd.lno]zMq (an ObscuredInt on an abstract backend singleton -
        // read everywhere the sponsor office / bike selection / team branding needs to
        // know the active team, e.g. State_SponsorOffice.cs, UI_BikeSelection.cs).
        // GameData.GetTeam(int) looks this value up against TeamInfo.teamID in
        // GameData's team list (D]nWNgg) to find the matching TeamInfo.
        //
        // DevCommandsBackEnd (a real dev-console class already in this build) exposes
        // a clean, plain "static int" wrapper property over the same field - no manual
        // ObscuredInt encode/decode needed, just reflection to get at the property
        // itself (its name still carries an obfuscated control character).
        //
        // TeamInfo itself is a clean, directly-referenceable type - teamID/name/tier
        // are all plain fields, no reflection needed for those.
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
                    MelonLogger.Warning("[CareerReset] Switch Sponsor: DevCommandsBackEnd.lno]zMq property not found.");
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
                MelonLogger.Warning("[CareerReset] Switch Sponsor: team list field (D]nWNgg) not found on GameData.");
                return new TeamInfo[0];
            }

            return teamsField.GetValue(gd) as TeamInfo[] ?? new TeamInfo[0];
        }

        private static int GetCurrentTeamId()
        {
            PropertyInfo p = GetCurrentTeamProp();
            if ((object)p == null) return 0;
            try { return (int)p.GetValue(null, null); }
            catch (Exception ex) { MelonLogger.Error("[CareerReset] GetCurrentTeamId: " + ex.Message); return 0; }
        }

        private static void SetCurrentTeamId(int id)
        {
            PropertyInfo p = GetCurrentTeamProp();
            if ((object)p == null) return;
            try { p.SetValue(null, id, null); }
            catch (Exception ex) { MelonLogger.Error("[CareerReset] SetCurrentTeamId: " + ex.Message); }
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
                catch (Exception ex) { MelonLogger.Error("[CareerReset] CurrentSponsorName: " + ex.Message); return "Error"; }
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
                    MelonLogger.Warning("[CareerReset] StepSponsor: no teams found - GameData not in scene yet?");
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
