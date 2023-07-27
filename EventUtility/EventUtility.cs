using MapGeneration;
using MEC;
using PlayerRoles;
using PluginAPI.Core;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InventorySystem.Items.Firearms;
using InventorySystem.Items.Usables.Scp330;
using Interactables.Interobjects.DoorUtils;

namespace TheRiptide
{
    public static class EventUtility
    {
        public static RoomIdentifier EndRoom;
        public static Vector3 RoomOffset = new Vector3(0.0f, 0.0f, 0.0f);
        public static int WinnerId = 0;
        public static CoroutineHandle RestartHandler;
        public static RoleTypeId LoserRole = RoleTypeId.NtfCaptain;

        public static void WinnerReset()
        {
            EndRoom = null;
            RoomOffset = new Vector3(0, 0, 0);
            WinnerId = 0;
            Timing.KillCoroutines(RestartHandler);
            LoserRole = RoleTypeId.NtfCaptain;
        }

        public static bool HandleGameOverRoleChange(Player player, RoleTypeId new_role)
        {
            int player_id = player.PlayerId;
            if (player_id == WinnerId)
            {
                if (new_role != RoleTypeId.ClassD && new_role != RoleTypeId.Spectator)
                {
                    Timing.CallDelayed(0.0f, () =>
                    {
                        Player p = Player.Get(player_id);
                        if (p != null)
                            p.SetRole(RoleTypeId.ClassD);
                    });
                    return false;
                }
            }
            else if (new_role != LoserRole && new_role != RoleTypeId.Spectator)
            {
                Timing.CallDelayed(0.0f, () =>
                {
                    Player p = Player.Get(player_id);
                    if (p != null)
                        p.SetRole(LoserRole);
                });
                return false;
            }
            return true;
        }

        public static void HandleGameOverSpawn(Player player)
        {
            int player_id = player.PlayerId;
            if (player_id == WinnerId)
            {
                Timing.CallDelayed(0.0f, () =>
                {
                    Player p = Player.Get(player_id);
                    if (p != null)
                    {
                        GrantWinnerReward(p);
                        Teleport.RoomPos(p, EndRoom, RoomOffset);
                    }
                });
            }
            else
            {
                Timing.CallDelayed(0.1f, () =>
                {
                    player.ClearInventory();
                    Teleport.RoomPos(player, EndRoom, RoomOffset);
                    //Player p = Player.Get(player_id);
                    //if (p != null)
                    //{
                    //    p.ClearInventory();
                    //    Teleport.RoomPos(p, EndRoom, RoomOffset);
                    //}
                });
            }
        }

        public static void FoundWinner(Player winner)
        {
            WinnerId = winner.PlayerId;
            winner.IsGodModeEnabled = true;
            winner.SendBroadcast("You Won!", 5, shouldClearPrevious: true);
            Timing.CallDelayed(5.0f, () =>
            {
                foreach (var p in Player.GetPlayers())
                {
                    if (p.PlayerId != WinnerId)
                        p.SetRole(LoserRole);
                    else
                    {
                        if (p.IsAlive)
                            GrantWinnerReward(p);
                        else
                            p.SetRole(RoleTypeId.ClassD);
                    }
                }
            });
            RestartHandler = Timing.CallDelayed(60.0f, () =>
            {
                Player p = Player.Get(WinnerId);
                if (p != null)
                    p.SetRole(RoleTypeId.Spectator);
            });
        }

        public static bool WinConditionLastClassD(Player victim)
        {
            if (!Round.IsRoundStarted)
                return false;
            bool found_winner = false;

            int dclass_alive = 0;
            foreach (var p in Player.GetPlayers())
                if (p.Role == RoleTypeId.ClassD)
                    dclass_alive++;
            if (dclass_alive == 0)
            {
                found_winner = true;
                WinnerId = victim.PlayerId;
            }
            else if (dclass_alive == 1)
            {
                found_winner = true;
                foreach (var p in Player.GetPlayers())
                {
                    if (p.Role == RoleTypeId.ClassD)
                    {
                        WinnerId = p.PlayerId;
                        p.IsGodModeEnabled = true;
                        p.SendBroadcast("You Won!", 5, shouldClearPrevious: true);
                    }
                }
            }

            if (found_winner)
            {
                Timing.CallDelayed(5.0f, () =>
                {
                    foreach (var p in Player.GetPlayers())
                    {
                        if (p.PlayerId != WinnerId)
                            p.SetRole(LoserRole);
                        else
                        {
                            if (p.IsAlive)
                                GrantWinnerReward(p);
                            else
                                p.SetRole(RoleTypeId.ClassD);
                        }
                    }
                });
                RestartHandler = Timing.CallDelayed(60.0f, () =>
                {
                    Player p = Player.Get(WinnerId);
                    if (p != null)
                        p.SetRole(RoleTypeId.Spectator);
                });
            }
            return found_winner;
        }

        public static void GrantWinnerReward(Player winner)
        {
            Teleport.RoomPos(winner, EndRoom, RoomOffset);
            winner.ClearInventory();
            winner.AddItem(ItemType.MicroHID);
            winner.AddItem(ItemType.GrenadeHE);
            winner.AddItem(ItemType.Jailbird);
            //ParticleDisruptor pd = winner.AddItem(ItemType.ParticleDisruptor) as ParticleDisruptor;
            //pd.Status = new FirearmStatus(50, pd.Status.Flags, pd.Status.Attachments);
            winner.AddItem(ItemType.SCP018);
            winner.AddItem(ItemType.SCP244a);
            Firearm gun = winner.AddItem(ItemType.GunCom45) as Firearm;
            gun.Status = new FirearmStatus(255, FirearmStatusFlags.Chambered, gun.Status.Attachments);
            winner.AddAmmo(ItemType.Ammo9x19, 2000);
            Scp330Bag bag = winner.AddItem(ItemType.SCP330) as Scp330Bag;
            bag.TryRemove(0);
            bag.TryAddSpecific(CandyKindID.Pink);
            bag.TryAddSpecific(CandyKindID.Pink);
            bag.TryAddSpecific(CandyKindID.Pink);
            bag.TryAddSpecific(CandyKindID.Pink);
            bag.TryAddSpecific(CandyKindID.Pink);
            bag.TryAddSpecific(CandyKindID.Pink);
            bag.ServerRefreshBag();
            winner.SendBroadcast("Now destroy the losers.\ncheck inv.", 30, shouldClearPrevious: true);
            winner.IsGodModeEnabled = true;
        }

        public static void ClearAllItems()
        {
            Timing.CallDelayed(1.0f, () =>
            {
                try
                {
                    Server.Instance.ReferenceHub.serverRoles.Permissions = (ulong)PlayerPermissions.FacilityManagement;
                    CommandSystem.Commands.RemoteAdmin.Cleanup.ItemsCommand cmd = new CommandSystem.Commands.RemoteAdmin.Cleanup.ItemsCommand();
                    string response = "";
                    string[] empty = { "" };
                    cmd.Execute(new System.ArraySegment<string>(empty, 0, 0), new RemoteAdmin.PlayerCommandSender(Server.Instance.ReferenceHub), out response);
                    ServerConsole.AddLog(response);
                }
                catch (System.Exception ex)
                {
                    ServerConsole.AddLog(ex.ToString());
                }
            });
        }

        public static void LockDownLight()
        {
            IEnumerable<DoorVariant> checkpoint_doors = DoorVariant.AllDoors.Where((d) => d.Rooms.Length == 1 && (d.Rooms[0].Name == RoomName.LczCheckpointA || d.Rooms[0].Name == RoomName.LczCheckpointB));
            foreach (DoorVariant checkpoint in checkpoint_doors)
                FacilityManager.LockDoor(checkpoint, DoorLockReason.AdminCommand);
        }

    }
}
