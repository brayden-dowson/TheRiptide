using PlayerStatsSystem;
using PluginAPI.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheRiptide
{
    public static class TraitorAmongUsUtility
    {
        public static string TauRoleToColor(TauRole role)
        {
            switch (role)
            {
                case TauRole.Unassigned: return "<color=#FFFFFF>";
                case TauRole.Innocent: return "<color=#00FF00>";
                case TauRole.Detective: return "<color=#0000FF>";
                case TauRole.Traitor: return "<color=#FF0000>";
                case TauRole.Jester: return "<color=#FF80FF>";
            }
            return "<color=#FFFFFF>";
        }

        //public static string KillReason(DamageHandlerBase handler)
        //{
        //    string reason = "Cause of death: Unknown.";
        //    if (handler is FirearmDamageHandler firearm_handler)
        //        reason = "Cause of death: Killed with a <color=#FF0000>" + firearm_handler.WeaponType.ToString().Replace("Gun", "") + ".</color>";
        //    else if (handler is ExplosionDamageHandler explosion_handler)
        //        reason = "Cause of death: <color=#FF0000>Explosive Grenade</color>";
        //    else if (handler is Scp018DamageHandler scp018_handler)
        //        reason = "Cause of death: <color=#FF0000>SCP018</color>";
        //    else if (handler is UniversalDamageHandler universal_handler)
        //        reason = "Cause of death: <color=#FF0000>" + DeathTranslations.TranslationsById[universal_handler.TranslationId].LogLabel + "</color>";
        //    return reason;
        //}
    }
}
