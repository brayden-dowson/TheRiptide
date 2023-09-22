using InventorySystem.Items;
using InventorySystem.Items.Firearms;
using MEC;
using Mirror;
using PlayerRoles;
using PlayerRoles.Ragdolls;
using PlayerStatsSystem;
using PluginAPI.Core;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static TheRiptide.Utility;
using static TheRiptide.TraitorAmongUsUtility;

namespace TheRiptide
{
    class IDGunManager
    {
        private static HashSet<ushort> id_guns = new HashSet<ushort>();
        private static CoroutineHandle update;

        public static void Start()
        {
            update = Timing.RunCoroutine(_IDGunUpdate());
        }

        public static void Stop()
        {
            Timing.KillCoroutines(update);
        }

        public static void Reset()
        {
            id_guns.Clear();
            ClearItemPickups();
        }

        public static void OnPlayerEquipItem(Player player, ItemBase item)
        {
            if (id_guns.Contains(item.ItemSerial))
                if (item is Revolver revolver)
                    if (revolver.Status.Attachments != 0)
                        revolver.Status = new FirearmStatus(0, FirearmStatusFlags.None, 0);
        }

        public static void GivePlayerIDGun(Player player)
        {
            Revolver revolver = player.AddItem(ItemType.GunRevolver) as Revolver;
            if (revolver != null)
            {
                revolver.Status = new FirearmStatus(0, FirearmStatusFlags.None, 0);
                id_guns.Add(revolver.ItemSerial);
            }
        }

        private static IEnumerator<float> _IDGunUpdate()
        {
            while(true)
            {
                try
                {
                    foreach(var player in Player.GetPlayers().Where(p => p.IsReady))
                    {
                        if (player.CurrentItem == null)
                            continue;
                        bool idgun = id_guns.Contains(player.CurrentItem.ItemSerial);
                        bool dna_scanner = player.CurrentItem.ItemTypeId == ItemType.MicroHID;
                        if (!idgun && !dna_scanner)
                            continue;

                        Vector3 start = player.ReferenceHub.PlayerCameraReference.position;
                        Vector3 dir = player.ReferenceHub.PlayerCameraReference.rotation * Vector3.forward;
                        RaycastHit[] hits_forward = Physics.RaycastAll(new Ray(start, dir), 3.5f, (1 << 28), QueryTriggerInteraction.Collide);
                        RaycastHit[] hits_backward = Physics.RaycastAll(new Ray(start + (dir * 3.5f), -dir), 3.5f, (1 << 28), QueryTriggerInteraction.Collide);
                        List<RaycastHit> hits = new List<RaycastHit>(hits_forward);
                        hits.AddRange(hits_backward);

                        foreach (var hit in hits)
                        {
                            SphereCollider sc = hit.collider as SphereCollider;
                            if (sc == null)
                                continue;

                            if (idgun)
                            {
                                if (!TraitorAmongUs.IsPlayerReady(player) && BodyManager.IsReadyUpBody(sc))
                                {
                                    TraitorAmongUs.ReadyUpPlayer(player);
                                    BodyManager.ReadyUpBody.IdedSingleClient(player);
                                }
                                else if (BodyManager.UnidedBody(sc) != null)
                                {
                                    IDableBody body = BodyManager.UnidedBody(sc);
                                    body.ServerIded();
                                    BodyManager.Unided.Remove(sc);
                                    TauRole scanner_role = TraitorAmongUs.GetPlayerTauRole(player);
                                    Announcements.Add(new Announcement("<color=#87ceeb><b>" + (scanner_role == TauRole.Detective ? TauRoleToColor(TauRole.Detective) : TauRoleToColor(TauRole.Innocent)) + player.Nickname + "</color></b> has found <b>" + body.Unided.Info.Nickname + "'s</b> body who" + (body.Real == TauRole.Innocent ? " was <b>" : " was a <b>") + TauRoleToColor(body.Real) + body.Real + "</b></color></color>", 30.0f));
                                    Announcements.RefreshInnocentInfo();
                                    if (TraitorAmongUs.traitors.Contains(player.PlayerId) || TraitorAmongUs.detectives.Contains(player.PlayerId))
                                        Shop.RewardCash(player, 50, "<b><color=#00FF00>$50</color> reward for IDing a body! Check shop for options</b>");
                                }
                                else if (TraitorAmongUs.detectives.Contains(player.PlayerId) && BodyManager.UnexaminedBody(sc) != null)
                                {
                                    IDableBody body = BodyManager.UnexaminedBody(sc);
                                    BodyManager.Unexamined.Remove(sc);
                                    Announcements.Add(new Announcement("<color=#87ceeb><b>" + TauRoleToColor(TauRole.Detective) + player.Nickname + "</color></b> has examined <b>" + TauRoleToColor(body.Real) + body.Unided.Info.Nickname + "'s</color></b> body. <color=#FFFF00>" + body.KillReason() + "</color></color>", 30.0f));
                                    Shop.RewardCash(player, 25, "<b><color=#00FF00>$25</color> reward for examining a body! Check shop for options</b>");
                                }
                            }
                            else if(dna_scanner)
                            {
                                if(BodyManager.UnscannedBody(sc) != null)
                                {
                                    IDableBody body = BodyManager.UnscannedBody(sc);
                                    BodyManager.Unscanned.Remove(sc);
                                    Announcements.Add(new Announcement("<color=#87ceeb><b>" + TauRoleToColor(TauRole.Detective) + player.Nickname + "</color></b> used DnA scanner on <b>" + TauRoleToColor(body.Real) + body.Unided.Info.Nickname + "'s</color></b> body. <color=#FF0000>Killed " + body.Attacker() + "</color></color>", 30.0f));
                                    Shop.RewardCash(player, 50, "<b><color=#00FF00>$50</color> reward for scanning a body! Check shop for options</b>");
                                }
                            }
                        }
                    }
                }
                catch(System.Exception ex)
                {
                    Log.Error(ex.ToString());
                }
                yield return Timing.WaitForSeconds(0.1f);
            }
        }

    }

    public class BodyManager
    {
        public static IDableBody ReadyUpBody = null;
        public static List<IDableBody> Bodies = new List<IDableBody>();
        public static Dictionary<SphereCollider, IDableBody> Unided = new Dictionary<SphereCollider, IDableBody>();
        public static Dictionary<SphereCollider, IDableBody> Unexamined = new Dictionary<SphereCollider, IDableBody>();
        public static Dictionary<SphereCollider, IDableBody> Unscanned = new Dictionary<SphereCollider, IDableBody>();
        private static System.Action<BasicRagdoll> on_ragdoll_spawned;

        public static void Start()
        {
            on_ragdoll_spawned = new System.Action<BasicRagdoll>((body) =>
            {
                try
                {
                    if ((ReadyUpBody != null && (ReadyUpBody.Unided == body || ReadyUpBody.IDed == body)) || Bodies.Any(b => b.Unided == body || b.IDed == body))
                        return;

                    TauRole killer_role = TauRole.Unassigned;
                    if (body.Info.Handler is AttackerDamageHandler attacker_handler)
                        killer_role = TraitorAmongUs.GetPlayerTauRole(Player.Get(attacker_handler.Attacker.Hub));

                    TauRole real_role = TraitorAmongUs.GetPlayerTauRole(Player.Get(body.Info.OwnerHub));
                    RoleTypeId death_role = RoleTypeId.None;
                    switch(real_role)
                    {
                        case TauRole.Traitor: death_role = RoleTypeId.Scientist; break;
                        case TauRole.Jester: death_role = (killer_role == TauRole.Traitor ? RoleTypeId.Scientist : RoleTypeId.Scp173); break;
                        case TauRole.Detective: death_role = RoleTypeId.NtfPrivate; break;
                        case TauRole.Unassigned: death_role = RoleTypeId.Scientist; break;
                        case TauRole.Innocent: death_role = RoleTypeId.Scientist; break;
                    }

                    NetworkServer.Destroy(body.gameObject);
                    if (real_role == TauRole.Unassigned)
                        return;

                    IDableBody idable_body = new IDableBody(body.Info.OwnerHub, body.Info.Handler, death_role, real_role, body.Info.StartPosition, body.Info.StartRotation, body.Info.Nickname);
                    Bodies.Add(idable_body);
                    Unided.Add(idable_body.Collider, idable_body);
                    Unexamined.Add(idable_body.Collider, idable_body);
                    Unscanned.Add(idable_body.Collider, idable_body);
                    idable_body.ServerSpawned();
                }
                catch (System.Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            });
            RagdollManager.OnRagdollSpawned += on_ragdoll_spawned;
        }

        public static void Stop()
        {
            Reset();
            RagdollManager.OnRagdollSpawned -= on_ragdoll_spawned;
        }

        public static void Reset()
        {
            Bodies.Clear();
            Unided.Clear();
            Unexamined.Clear();
            Unscanned.Clear();
            ClearRagdolls();
        }

        public static bool IsReadyUpBody(SphereCollider collider)
        {
            return ReadyUpBody != null && ReadyUpBody.Collider == collider;
        }

        public static IDableBody UnidedBody(SphereCollider collider)
        {
            IDableBody result = null;
            Unided.TryGetValue(collider, out result);
            return result;
        }

        public static IDableBody UnexaminedBody(SphereCollider collider)
        {
            IDableBody result = null;
            Unexamined.TryGetValue(collider, out result);
            return result;
        }

        public static IDableBody UnscannedBody(SphereCollider collider)
        {
            IDableBody result = null;
            Unscanned.TryGetValue(collider, out result);
            return result;
        }

        public static void RespawnReadyupBodyForClient(Player player)
        {
            if(ReadyUpBody != null)
                NetworkServer.SendSpawnMessage(ReadyUpBody.Unided.netIdentity, player.Connection);
        }

        public static void BeginReadyUP(Vector3 position)
        {
            ReadyUpBody = new IDableBody(Server.Instance.ReferenceHub, new UniversalDamageHandler(), RoleTypeId.Scientist, TauRole.Innocent, position, Quaternion.Euler(-90.0f, 0.0f, 0.0f), "\n<b><color=#FF0000>ID body with ID-Gun to ready up<b></color>\n");
            ReadyUpBody.ServerSpawned();
        }

        public static void EndReadyUp()
        {
            ReadyUpBody.Destroy();
            ReadyUpBody = null;
        }
    }

    public class IDableBody
    {
        private static GameObject dcpf = null;
        public SphereCollider Collider;
        public BasicRagdoll Unided;
        public BasicRagdoll IDed;
        public TauRole Real;
        public DamageHandlerBase handler;

        public IDableBody(ReferenceHub owner, DamageHandlerBase handler, RoleTypeId original, TauRole real, Vector3 pos, Quaternion rot, string msg)
        {
            this.handler = handler;
            if (dcpf == null)
            {
                dcpf = new GameObject("detector_collider", new System.Type[] { typeof(SphereCollider) });
                dcpf.GetComponent<SphereCollider>().radius = 1.5f;
                dcpf.layer = 28;
            }
            Collider = Object.Instantiate(dcpf, pos + Vector3.down, Quaternion.identity).GetComponent<SphereCollider>();
            Real = real;
            RoleTypeId ided_role = RoleTypeId.None;
            switch (real)
            {
                case TauRole.Unassigned: ided_role = RoleTypeId.None; break;
                case TauRole.Innocent: ided_role = RoleTypeId.NtfSpecialist; break;
                case TauRole.Detective: ided_role = RoleTypeId.NtfCaptain; break;
                case TauRole.Traitor: ided_role = RoleTypeId.Tutorial; break;
                case TauRole.Jester: ided_role = RoleTypeId.Scp173; break;
            }
            if (ided_role == RoleTypeId.None)
                return;

            Unided = CreateRagdoll(owner, original, pos, rot, msg);
            IDed = CreateRagdoll(Server.Instance.ReferenceHub, ided_role, pos, rot, Unided.Info.Nickname + (ided_role == RoleTypeId.NtfSpecialist ? " was " : " was a ") + real);
            IDed.gameObject.SetActive(false);
        }

        public void ServerSpawned()
        {
            NetworkServer.Spawn(Unided.gameObject);
        }

        public void ServerIded()
        {
            NetworkServer.Destroy(Unided.gameObject);
            IDed.gameObject.SetActive(true);
            NetworkServer.Spawn(IDed.gameObject);
        }

        public void IdedSingleClient(Player client)
        {
            client.Connection.Send(new ObjectDestroyMessage { netId = Unided.netId });
            NetworkServer.SendSpawnMessage(IDed.netIdentity, client.Connection);
        }

        public void Destroy()
        {
            NetworkServer.SendToAll(new ObjectDestroyMessage { netId = Unided.netId });
            NetworkServer.SendToAll(new ObjectDestroyMessage { netId = IDed.netId });
        }

        public string KillReason()
        {
            string reason = "Cause of death: Unknown.";
            if (handler is FirearmDamageHandler firearm_handler)
                reason = "Cause of death: Killed with a <color=#FF0000>" + firearm_handler.WeaponType.ToString().Replace("Gun", "") + ".</color>";
            else if (handler is ExplosionDamageHandler explosion_handler)
                reason = "Cause of death: <color=#FF0000>Explosive Grenade</color>";
            else if (handler is Scp018DamageHandler scp018_handler)
                reason = "Cause of death: <color=#FF0000>SCP018</color>";
            else if (handler is UniversalDamageHandler universal_handler)
                reason = "Cause of death: <color=#FF0000>" + DeathTranslations.TranslationsById[universal_handler.TranslationId].LogLabel + "</color>";
            return reason;
        }

        public string Attacker()
        {
            string attacker = "him self";
            if(handler is AttackerDamageHandler attacker_handler && !attacker_handler.IsSuicide)
                attacker = "by " + attacker_handler.Attacker.Nickname;
            return attacker;
        }
    }
}
