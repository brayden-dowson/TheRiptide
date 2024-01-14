using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheRiptide
{
    public static class StaticTranslation
    {
        public static TranslationConfig Translation;
    }

    public class TranslationConfig
    {
        public string NpcBracketName { get; set; } = "[Tournament Bracket]";

        public string OutOfBounds { get; set; } = "<b><color=#FF0000>You are out of bounds!</b></color>";

        public string VoteBanFormat { get; set; } = "<size=29><color=#ff8a62>Bans - " +
                "</color><color=#b6ff61>Your votes: {your_votes}\n" +
                "</color><color=#ffb84c>Team votes: <color=#ffb84c>{team_votes}</color>\n" +
                "</color><color=#ff6565>Item Bans: <color=#ff6565>{team_bans}</color>\n" +
                "</color><color=#f5ff5b>Zones: <color=#b6ff61>[{your_zone_vote}]</color> <color=#ffb84c>Votes: {team_zone_votes}</color> <color=#ff6565>Ban: {zone_bans}";

        public string BracketGlitchWarning { get; set; } = "If this appears midgame type .ch in your game console or press p to disable your HUD";

        public string LoadoutFormat { get; set; } = "<color=#b7eb8f>Loadout\n" +
            "<color=#87e8de>Weapon:</color> {weapon}" +
            " <color=#87e8de>Medical:</color> {medical}" +
            "\n<color=#87e8de> Candy:</color> {candy}" +
            " <color=#87e8de> SCP:</color> {scp}" +
            " <color=#87e8de> Other:</color> {other}{extra}";

        public string ExtraStringZone { get; set; } = "\n<color=#87e8de>Zone: </color><color=#b7eb8f>{zone}</color>";

        public string ExtraStringTeamItemLimitHit { get; set; } = "\nYour team has reached the limit for {type} of {limit}";

        public string WaitingForZone { get; set; } = "Waiting for Zone {zone}";

        public string Draw { get; set; } = "<b>Draw</b>\n";
        public string Won { get; set; } = "<b>You Won!</b>\n";
        public string Lost { get; set; } = "<b>You Lost</b>\n";

        public string YouAreReady { get; set; } = "<color=#00FF00>YOU ARE READY</color>";
        public string YouAreNotReady { get; set; } = "<color=#FF0000>YOU ARE NOT READY</color>";
        public string TimeLeftFormat { get; set; } = "\n<color=#87ceeb>{time}</color>";

        [Description("Translation for Role Sync Teams")]
        public string TeamAssigned { get; set; } = "<color=#00bbff>Team:</color> <b>{team}</color></b>";
        public string TeamMissingOut { get; set; } = "<b><color=#FF0000>Your team: {local_group} is not apart of the predefined bracket for this tournament if you believe this is an error speak to a tournament organiser";
        public string LocalGroupError { get; set; } = "Error could not get UserGroup from local_group try rejoining server and contact a tournament organiser";
        public string PlayerFailureToLinkCedmod { get; set; } = "<b><color=#FF0000>Could not assign a team because you have either\n1. Not linked your dicord to steam\n2. Not logged into Cedmod";
        [Description("#")]

        public string ScrimmageAssignedTeamFormat { get; set; } = "Scrimmage Mode - You have been assigned to: <b>{team}</b>";
        public string TeamSurrendered { get; set; } = "\n<b>{team}</b></color> Surrendered";
        public string MatchWon { get; set; } = "<b>{winner_team}</b></color> Defeated <b>{loser_team}</b></color> {winner_score} - {loser_score}{reason}";
        public string TournamentWon { get; set; } = "<size=64><b>{team} Won!</color></b></size>";
    }
}
