﻿using System;
using UnityEngine;
using VRC;
using VRC.Core;
using VRCSDK2.Validation.Performance;

namespace PlayerList.Entries
{
    class LocalPlayerEntry : PlayerEntry
    {
        // " - <color={pingcolor}>{ping}ms</color> | <color={fpscolor}>{fps}</color> | {platform} | <color={perfcolor}>{perf}</color> | {relationship} | <color={rankcolor}>{displayname}</color>"
        public override string Name { get { return "Local Player"; } }

        public new delegate void UpdateEntryDelegate(Player player, LocalPlayerEntry entry, ref string tempString);
        public static new UpdateEntryDelegate updateDelegate;
        public override void Init(object[] parameters)
        {
            player = Player.prop_Player_0;
            gameObject.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(new Action(() => OpenPlayerInQuickMenu(player)));

            platform = GetPlatform(player).PadRight(2);

            NetworkHooks.OnPlayerJoin += new Action<Player>((player) =>
            {
                int highestId = 0;
                foreach (int photonId in PlayerManager.prop_PlayerManager_0.field_Private_Dictionary_2_Int32_Player_0.Keys)
                    if (photonId > highestId)
                        highestId = photonId;

                highestPhotonIdLength = highestId.ToString().Length;
            });

            textComponent.text = "Loading...";

            OnConfigChanged();
        }
        public override void OnConfigChanged()
        {
            updateDelegate = null;
            if (Config.pingToggle.Value)
                updateDelegate += AddPing;
            if (Config.fpsToggle.Value)
                updateDelegate += AddFps;
            if (Config.platformToggle.Value)
                updateDelegate += AddPlatform;
            if (Config.perfToggle.Value)
                updateDelegate += AddPerf;
            if (Config.distanceToggle.Value)
                updateDelegate += AddDistance;
            if (Config.photonIdToggle.Value)
                updateDelegate += AddPhotonId;
            if (Config.displayNameToggle.Value)
                updateDelegate += AddDisplayName;
        }
        protected override void ProcessText(object[] parameters)
        {
            // TODO: Figure out how to figure out how to know when someone blcosk u
            /*
            List<PlayerEntry> playerEntries = PlayerListMod.playerEntries.Values.ToList();
            // Get blocked things
            foreach (ApiPlayerModeration moderation in ModerationManager.prop_ModerationManager_0.field_Private_List_1_ApiPlayerModeration_0)
            {
                if (moderation.moderationType != ApiPlayerModeration.ModerationType.Block) continue;

                foreach (PlayerEntry playerEntry in playerEntries)
                {
                    if (playerEntry.youBlocked) continue;

                    if (playerEntry.player == null)
                    {
                        playerEntry.Remove();
                        continue;
                    }

                    if (playerEntry.userID == moderation.targetUserId)
                    { 
                        playerEntry.youBlocked = true;
                        MelonLoader.MelonLogger.Msg($"You have blocked {moderation.targetDisplayName}");
                        break;
                    }
                }
            }
            */
            string tempString = "";

            player = Player.prop_Player_0;
            updateDelegate?.Invoke(player, this, ref tempString);

            tempString = AddLeftPart(tempString);
            textComponent.text = tempString;
        }

        private static void AddPing(Player player, LocalPlayerEntry entry, ref string tempString)
        {
            short ping = (short)Photon.Pun.PhotonNetwork.field_Public_Static_LoadBalancingClient_0.prop_LoadBalancingPeer_0.RoundTripTime;
            tempString += "<color=" + GetPingColor(ping) + ">";
            if (ping < 9999 && ping > -999)
                tempString += ping.ToString().PadRight(4) + "ms</color>";
            else
                tempString += ((double)(ping / 1000)).ToString("N1").PadRight(5) + "s</color>";
            tempString += separator;
        }
        private static void AddFps(Player player, LocalPlayerEntry entry, ref string tempString)
        {
            int fps = Mathf.Clamp((int)(1f / Time.deltaTime), -99, 999); // Clamp between -99 and 999
            tempString += "<color=" + GetFpsColor(fps) + ">" + fps.ToString().PadRight(3) + "</color>" + separator;
        }
        private static void AddPlatform(Player player, LocalPlayerEntry entry, ref string tempString)
        {
            tempString += entry.platform + separator;
        }
        private static void AddPerf(Player player, LocalPlayerEntry entry, ref string tempString)
        {
            PerformanceRating rating = player.field_Internal_VRCPlayer_0.prop_VRCAvatarManager_0.prop_AvatarPerformanceStats_0.field_Private_ArrayOf_PerformanceRating_0[(int)AvatarPerformanceCategory.Overall]; // Get from cache so it doesnt calculate perf all at once
            if (rating != entry.lastPerf)
                EntryManager.shouldSort = true;
            entry.lastPerf = rating;
            tempString += "<color=#" + ColorUtility.ToHtmlStringRGB(VRCUiAvatarStatsPanel.Method_Private_Static_Color_AvatarPerformanceCategory_PerformanceRating_0(AvatarPerformanceCategory.Overall, rating)) + ">" + ParsePerformanceText(rating) + "</color>" + separator;
        }
        private static void AddDistance(Player player, LocalPlayerEntry entry, ref string tempString)
        {
            tempString += "0.0 m" + separator;
        }
        private static void AddPhotonId(Player player, LocalPlayerEntry entry, ref string tempString)
        {
            tempString += player.field_Internal_VRCPlayer_0.field_Private_PhotonView_0.field_Private_Int32_0.ToString().PadRight(highestPhotonIdLength) + separator;
        }
        private static void AddDisplayName(Player player, LocalPlayerEntry entry, ref string tempString)
        {
            if (reCacheColor || entry.playerColor == "#")
            {
                entry.playerColor = "";
                switch (Config.DisplayNameColorMode)
                {
                    case DisplayNameColorMode.None:
                    case DisplayNameColorMode.FriendsOnly:
                        break;
                    case DisplayNameColorMode.TrustAndFriends:
                    case DisplayNameColorMode.TrustOnly:
                        entry.playerColor = "#" + ColorUtility.ToHtmlStringRGB(VRCPlayer.Method_Public_Static_Color_APIUser_0(APIUser.CurrentUser));
                        break;
                }
            }
            tempString += "<color=" + entry.playerColor + ">" + player.field_Private_APIUser_0.displayName + "</color>" + separator;
        }
    }
}
