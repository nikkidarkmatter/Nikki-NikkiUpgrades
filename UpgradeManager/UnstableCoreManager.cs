using UnityEngine;
using REPOLib.Modules;
using static NikkiUpgrades.NikkiUpgradesPlugin;
using Unity.VisualScripting;
using Photon.Pun;

namespace NikkiUpgrades.UpgradeManager
{
    public class UnstableCoreManager : MonoBehaviour
    {
        [HideInInspector]
        public PlayerAvatar player;

        private SyncedEventRandom syncedRandomNumber;

        private bool explosionTriggered = false;

        private GameObject explosion;

        private UnstableCoreExplosion explosionScript;

        private PhotonView photon;

        private void Start()
        {
            photon = GetComponent<PhotonView>();
            syncedRandomNumber = gameObject.GetOrAddComponent<SyncedEventRandom>();
            player = GetComponent<PlayerAvatar>();
            explosion = NikkiUpgradesPlugin.unstableexplosion;
            explosionTriggered = false;
        }
        private void FixedUpdate()
        {
            if (SemiFunc.RunIsLobbyMenu() || SemiFunc.IsMainMenu())
            {
                return;
            }
            if (!player.deadSet && !player.isTumbling && explosionTriggered)
            {
                explosionTriggered = false;
                ExplodeResetImpulse();
                return;
            }
            if (Upgrades.GetUpgrade("UnstableCore").GetLevel(player) >= 3)
            {
                if (unstableUpgradeInstability.Value)
                {
                    if (player.playerHealth.hurtFreeze)
                    {
                        syncedRandomNumber.RandomRangeInt(1, 1000);
                        if (syncedRandomNumber.resultRandomRangeInt <= Upgrades.GetUpgrade("UnstableCore").GetLevel(player))
                        {
                            ExplodeImpulse();
                        }
                    }
                }
                if (unstableUpgradeTumbleMissile.Value)
                {
                    player.tumble.hurtCollider.onImpactEnemy.AddListener(ExplodeImpulse);
                    player.tumble.hurtCollider.onImpactPlayer.AddListener(ExplodeImpulse);
                }
            }
            if (player.deadSet && !explosionTriggered)
            {
                ExplodeImpulse();
            }
            if (Upgrades.GetUpgrade("UnstableCore").GetLevel(player) == 0)
            {
                Destroy(this);
            }
        }

        public void ExplodeImpulse()
        {
            if (SemiFunc.IsMasterClientOrSingleplayer())
            {
                if (SemiFunc.IsMultiplayer())
                {
                    photon.RPC("ExplodeRPC", RpcTarget.All);
                }
                else
                {
                    Explode();
                }
            }
        }

        [PunRPC]
        public void ExplodeRPC()
        {
            Explode();
        }

        public void Explode()
        {
            if (!explosionTriggered)
            {
                if (SemiFunc.IsMasterClientOrSingleplayer())
                {
                    explosion = NetworkPrefabs.SpawnNetworkPrefab("UnstableExplosion", this.transform.position, this.transform.rotation);
                }
                explosionScript = explosion.GetComponent<UnstableCoreExplosion>();
                explosionScript.player = this.player;
                explosionTriggered = true;
            }
        }

        public void ExplodeResetImpulse()
        {
                if (SemiFunc.IsMultiplayer())
                {
                    photon.RPC("ExplodeResetRPC", RpcTarget.All);
                }
                else
                {
                    explosionTriggered = false;
                }
        }

        [PunRPC]
        public void ExplodeResetRPC()
        {
            explosionTriggered = false;
        }

        private void OnDestroy()
        {
            if (player != null && player.tumble != null && player.tumble.hurtCollider != null)
            {
                player.tumble.hurtCollider.onImpactEnemy.RemoveListener(Explode);
                player.tumble.hurtCollider.onImpactPlayer.RemoveListener(Explode);
            }
        }
    }
    public class UnstableCoreExplosion : MonoBehaviour
    {   
        public ParticleSystem particleExplosion;

        public HurtCollider hurtCollider;

        public Sound explosionSound;

        public Sound explosionSoundGlobal;

        public PlayerAvatar player;

        private PhotonView photonView;

        private float explosionTime = 7.5f;

        private void Start()
        {
            photonView = GetComponent<PhotonView>();
            explosionTime = 7.5f;
            if (player == null)
            {
                mls.LogError("Error passing along player! Grabbing local value as failsafe! This will likely cause unintended side effects if not in singleplayer!");
                player = SemiFunc.PlayerAvatarLocal();
            }
            mls.LogDebug("Spawning explosion!");
            var level = Upgrades.GetUpgrade("UnstableCore").GetLevel(player);
            var explosionDamage = unstableUpgradeDamage.Value * level;
            this.gameObject.transform.localScale = (Vector3.one * (unstableUpgradeRadius.Value * 2) * level);
            mls.LogDebug($"Explosion with diameter {this.gameObject.transform.localScale} and {explosionDamage} damage spawned at {this.gameObject.transform.position}!");
            ParticleSystem.MainModule explosion = particleExplosion.main;
            hurtCollider.playerDamage = (int)Mathf.Round(explosionDamage * 0.4f);
            hurtCollider.enemyDamage = (int)explosionDamage;
            explosion.startSpeedMultiplier = 0.5f + (0.5f * level);
            explosion.m_ParticleSystem.Play();
            explosionSound.Play(this.gameObject.transform.position, 2, 0.2f, 2, 0.4f);
            explosionSoundGlobal.Play(this.gameObject.transform.position, 2, 0.2f, 2, 0.4f);
            SemiFunc.CameraShakeDistance(this.gameObject.transform.position, level * 10f, 1f + (level * 0.5f), unstableUpgradeRadius.Value * 2, unstableUpgradeRadius.Value * 5);
        }

        private void Update()
        {
            explosionTime -= Time.deltaTime;
            if (explosionTime <= 7.3f && hurtCollider.enabled)
            {
                mls.LogDebug($"Disabling HurtCollider.");
                hurtCollider.enabled = false;
            }
            if (explosionTime <= 0f)
            {
                mls.LogDebug($"Destroying explosion GameObject.");
                Destroy(this);
            }
        }
    }
}
