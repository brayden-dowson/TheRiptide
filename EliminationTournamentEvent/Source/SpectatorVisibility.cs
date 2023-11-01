using CustomPlayerEffects;
using InventorySystem.Items.Usables.Scp1576;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.Visibility;
using PluginAPI.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TheRiptide
{
    //class CustomVisibilityController : VisibilityController
    //{
    //    private const int SurfaceHeight = 800;
    //    private Invisible _invisibleEffect;
    //    private Match match;

    //    protected virtual int NormalMaxRangeSqr => 1300;
    //    protected virtual int SurfaceMaxRangeSqr => 4900;

    //    public CustomVisibilityController(Match match, ReferenceHub owner)
    //    {
    //        Owner = owner;
    //        Role = owner.roleManager.CurrentRole;
    //        _invisibleEffect = owner.playerEffectsController.GetEffect<Invisible>();
    //        this.match = match;
    //    }

    //    public override InvisibilityFlags GetActiveFlags(ReferenceHub observer)
    //    {
    //        InvisibilityFlags activeFlags = base.GetActiveFlags(observer);
    //        if (_invisibleEffect.IsEnabled)
    //            activeFlags |= InvisibilityFlags.Scp268;
    //        if (!(observer.roleManager.CurrentRole is IFpcRole currentRole1) || !(Owner.roleManager.CurrentRole is IFpcRole currentRole2))
    //        {
    //            if (Owner.roleManager.CurrentRole is IFpcRole target_role && match != null && match.OnOppositeTeams(Owner, observer))
    //                activeFlags |= InvisibilityFlags.OutOfRange;
    //            return activeFlags;
    //        }
    //        Vector3 position1 = currentRole1.FpcModule.Position;
    //        Vector3 position2 = currentRole2.FpcModule.Position;
    //        float num = Mathf.Min(position1.y, position2.y) > 800.0 ? SurfaceMaxRangeSqr : NormalMaxRangeSqr;
    //        if ((position1 - position2).sqrMagnitude > num)
    //            activeFlags |= InvisibilityFlags.OutOfRange;
    //        return activeFlags;
    //    }

    //    public override void SpawnObject()
    //    {
    //        base.SpawnObject();
    //        _invisibleEffect = Owner.playerEffectsController.GetEffect<Invisible>();
    //    }
    //}

    public static class SpectatorVisibility
    {
        class Matchup
        {
            public Team team;
            public Team opposition;
        }
        //private static Dictionary<string, Team> player_opposition = new Dictionary<string, Team>();
        private static Dictionary<string, Matchup> player_matchup = new Dictionary<string,  Matchup>();

        public static void SetMatchup(Player player, Team team, Team opposition)
        {
            if (!player_matchup.ContainsKey(player.UserId))
                player_matchup.Add(player.UserId, new Matchup { team = team, opposition = opposition });
            else
                player_matchup[player.UserId] = new Matchup { team = team, opposition = opposition };
        }

        public static void RemoveMatchup(Player player)
        {
            player_matchup.Remove(player.UserId);
        }

        public static bool AllowSpectating(ReferenceHub observer, ReferenceHub target)
        {
            if (player_matchup.ContainsKey(target.authManager.UserId))
            {
                Matchup matchup = player_matchup[target.authManager.UserId];
                if(matchup.opposition.Users.Contains(observer.authManager.UserId))
                {
                    //if (matchup.team.Users.Any((u) => { Player p; if (Player.TryGet(u, out p)) return UsingScp1576(p); return false; }))
                    //    return true;
                    return false;
                }
            }
            return true;
        }

        private static bool UsingScp1576(Player player)
        {
            return Scp1576Item.ValidatedTransmitters.Contains(player.ReferenceHub);
        }

        public static bool InMatch(ReferenceHub hub)
        {
            return player_matchup.ContainsKey(hub.authManager.UserId);
        }

    }
}
