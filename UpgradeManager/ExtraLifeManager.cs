/* using UnityEngine;
using REPOLib.Modules;
using System;

namespace NikkiUpgrades.UpgradeManager
{
    public class ExtraLifeManager : MonoBehaviour
    {
        private PlayerAvatar player;

        private int extraLifeCount;

        private float reviveTime;

        private bool reviveStarted = false;

        private ParticleSystem reviveParticles;

        private void Start()
        {
            player = GetComponent<PlayerAvatar>();
            extraLifeCount = Upgrades.GetUpgrade("LifeInsurance").GetLevel(player);
            if (extraLifeCount == 0)
            {
                Destroy(this);
            }
            if (SemiFunc.IsMasterClientOrSingleplayer())
            {
                reviveTime = UnityEngine.Random.Range(5f, 10f);
            }
        }

        private void Update()
        {
            if (RunManager.instance.allPlayersDead)
            {
                RunManager.instance.allPlayersDead = false;
                RunManager.instance.restarting = true;
                RunManager.instance.restartingDone = false;
            }
            if (player.deadTime > 0 && !reviveStarted)
            {
                player.playerReviveEffects.enableTransform.gameObject.SetActive(true);
                reviveParticles = player.playerReviveEffects.swirlParticle;
                ParticleSystem.MainModule main = reviveParticles.main;
                main.loop = true;
                reviveParticles.Play();
                reviveStarted = true;
            }
            if (player.deadTime >= reviveTime)
            {
                ParticleSystem.MainModule main = reviveParticles.main;
                main.loop = false;
                if (SemiFunc.IsMasterClientOrSingleplayer())
                {
                    player.Revive();
                    // Upgrades.GetUpgrade("LifeInsurance").RemoveLevel(player);
                }
                extraLifeCount--;
                reviveStarted = false;
                RunManager.instance.restarting = false;
                if (extraLifeCount == 0)
                {
                    Destroy(this);
                }
            }
        }
    }
}
*/