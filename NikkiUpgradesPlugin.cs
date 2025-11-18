using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using REPOLib.Modules;
using System.IO;
using UnityEngine;
using System;
using NikkiUpgrades.UpgradeManager;

namespace NikkiUpgrades
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInDependency("REPOLib", BepInDependency.DependencyFlags.HardDependency)]
    public class NikkiUpgradesPlugin : BaseUnityPlugin
    {
        public static ManualLogSource mls;

        public static ConfigEntry<bool> staminaRegenUpgradeEnabled;
        public static ConfigEntry<float> staminaRegenUpgradeAmount;

        public static ConfigEntry<bool> investorUpgradeEnabled;
        public static ConfigEntry<float> investorUpgradeAmount;
        public static ConfigEntry<int> investorUpgradeMax;

        // public static ConfigEntry<bool> lifeInsuranceUpgradeEnabled;

        public static ConfigEntry<bool> unstableUpgradeEnabled;
        public static ConfigEntry<int> unstableUpgradeDamage;
        public static ConfigEntry<float> unstableUpgradeRadius;
        public static ConfigEntry<bool> unstableUpgradeInstability;
        public static ConfigEntry<bool> unstableUpgradeTumbleMissile;

        public static PlayerUpgrade staminaRegenRegister;
        public static PlayerUpgrade investorRegister;
        // public static PlayerUpgrade lifeInsuranceRegister;
        public static PlayerUpgrade unstableCoreRegister;

        private static float initialStamina;
        private static bool initialStaminaSet = false;

        public static bool investorUsed = false;
        public static bool investorTriggered = false;

        public static GameObject unstableexplosion;

        private readonly Harmony harmony = new Harmony("nikkiupgrades.mod");
        private void Awake()
        {
            mls = BepInEx.Logging.Logger.CreateLogSource(MyPluginInfo.PLUGIN_GUID);

            string pluginFolderPath = Path.GetDirectoryName(Info.Location);
            string assetBundleFilePath = Path.Combine(pluginFolderPath, "nikkiupgrades_assets");
            AssetBundle assetBundle = AssetBundle.LoadFromFile(assetBundleFilePath);

            investorUpgradeEnabled = Config.Bind("Investor Upgrade", "Enabled", true, "Should the Investor upgrade be enabled?");
            investorUpgradeAmount = Config.Bind("Investor Upgrade", "Investor Upgrade Power", 10f, "How much should the Investor upgrade multiply your total money, per upgrade? Base value is 10%.");
            investorUpgradeMax = Config.Bind("Investor Upgrade", "Investor Upgrade Purchase Maximum", 3, "How many Investor upgrades should be purchasable before they stop spawning? Base value is 3.");

            staminaRegenUpgradeEnabled = Config.Bind("Stamina Regen Upgrade", "Enabled", true, "Should the Stamina Regen Upgrade be enabled?");
            staminaRegenUpgradeAmount = Config.Bind("Stamina Regen Upgrade", "Stamina Regen Upgrade Power", 1f, "How much should Stamina Regen increase per upgrade? Base stamina regen is 2. The base value means it takes 2 upgrades to double your stamina regen.");

            // lifeInsuranceUpgradeEnabled = Config.Bind("Life Insurance Upgrade", "Enabled", true, "Should the Life Insurance Upgrade be enabled?");

            unstableUpgradeEnabled = Config.Bind("Unstable Core Upgrade", "Enabled", true, "Should the Unstable Core Upgrade be enabled?");
            unstableUpgradeDamage = Config.Bind("Unstable Core Upgrade", "Unstable Core Upgrade Power", 250, "How much damage should the Unstable Core Upgrade do per upgrade? Base value is 250. This value is multiplied by 0.4 for players (default 100).");
            unstableUpgradeRadius = Config.Bind("Unstable Core Upgrade", "Unstable Core Upgrade Radius", 3f, "How big should the radius of the Unstable Core Upgrade explosion be? Base value is 3f.");
            unstableUpgradeInstability = Config.Bind("Unstable Core Upgrade", "Unstable Core Upgrade Stability", true, "Should the Unstable Core upgrade become more dangerous and unstable after 3+ upgrades on the same player?");
            unstableUpgradeTumbleMissile = Config.Bind("Unstable Core Upgrade", "Unstable Core Upgrade Tumble Missile", true, "Should the Unstable Core upgrade allow you to become a tumble missile after 3+ upgrades on the same player?");

            harmony.PatchAll(typeof(NikkiUpgradesPlugin));

            if (staminaRegenUpgradeEnabled.Value)
            {
                GameObject staminaregenObject = assetBundle.LoadAsset<GameObject>("Item Upgrade Player Stamina Regen");
                ItemAttributes staminaregenitem = staminaregenObject.GetComponent<ItemAttributes>();
                Items.RegisterItem(staminaregenitem);
                staminaRegenRegister = Upgrades.RegisterUpgrade("StaminaRegen", staminaregenitem.item, InitStaminaUpgrade, UseStaminaUpgrade);
            }
            if (investorUpgradeEnabled.Value)
            {
                GameObject investorObject = assetBundle.LoadAsset<GameObject>("Item Upgrade Player Investor");
                ItemAttributes investoritem = investorObject.GetComponent<ItemAttributes>();
                GameObject investormanager = assetBundle.LoadAsset<GameObject>("Investor Manager");
                Items.RegisterItem(investoritem);
                NetworkPrefabs.RegisterNetworkPrefab("investormanager", investormanager);
                investorRegister = Upgrades.RegisterUpgrade("Investor", investoritem.item, InitInvestorUpgrade, UseInvestorUpgrade);
                investoritem.item.maxPurchaseAmount = investorUpgradeMax.Value;
            }
            /* if (lifeInsuranceUpgradeEnabled.Value)
            {
                Item lifeinsuranceitem = assetBundle.LoadAsset<Item>("Item Upgrade Player Life Insurance");
                Items.RegisterItem(lifeinsuranceitem);
                lifeInsuranceRegister = Upgrades.RegisterUpgrade("LifeInsurance", lifeinsuranceitem, InitExtraLifeUpgrade, UseExtraLifeUpgrade);
            } */
            if (unstableUpgradeEnabled.Value)
            {
                GameObject unstablecoreObject = assetBundle.LoadAsset<GameObject>("Item Upgrade Player Unstable Core");
                ItemAttributes unstablecoreitem = unstablecoreObject.GetComponent<ItemAttributes>();
                unstableexplosion = assetBundle.LoadAsset<GameObject>("Unstable Explosion");
                NetworkPrefabs.RegisterNetworkPrefab("UnstableExplosion", unstableexplosion);
                Items.RegisterItem(unstablecoreitem);
                // NetworkPrefabs.RegisterNetworkPrefab("unstableexplosion", unstableexplosion);
                unstableCoreRegister = Upgrades.RegisterUpgrade("UnstableCore", unstablecoreitem.item, InitUnstableUpgrade, UseUnstableUpgrade);
            }
        }

        private static void InitStaminaUpgrade(PlayerAvatar player, int level)
        {
            if (!player.isLocal)
            {
                return;
            }
            PlayerController playerController = PlayerController.instance;
            Traverse playerTraverse = Traverse.Create(playerController).Field("sprintRechargeAmount");
            if (!initialStaminaSet)
            {
                initialStaminaSet = true;

                initialStamina = (float)playerTraverse.GetValue();
            }
            playerTraverse.SetValue(CalculateStaminaRegen(level));
        }
        private static void UseStaminaUpgrade(PlayerAvatar player, int level)
        {
            if (!player.isLocal)
            {
                return;
            }
            PlayerController playerController = PlayerController.instance;
            Traverse playerTraverse = Traverse.Create(playerController).Field("sprintRechargeAmount");
            float newValue = CalculateStaminaRegen(level);
            mls.LogDebug($"Sprint Recharge Amount now at {newValue}.");
            playerTraverse.SetValue(newValue);
        }

        private static float CalculateStaminaRegen(int level)
        {
            return initialStamina + (staminaRegenUpgradeAmount.Value * level);
        }

        private static void InitInvestorUpgrade(PlayerAvatar player, int level)
        {
            if (SemiFunc.IsMasterClientOrSingleplayer() && (SemiFunc.RunIsLobby() || SemiFunc.RunIsShop()))
            {
                NetworkPrefabs.SpawnNetworkPrefab(NetworkPrefabs.GetNetworkPrefabRef("investormanager"), Vector3.zero, Quaternion.identity);
            }
        }

        private static void UseInvestorUpgrade(PlayerAvatar player, int level)
        {
            investorUsed = true;
            if (!SemiFunc.RunIsLobby())
            {
                return;
            }
            int preInvestment = SemiFunc.StatGetRunCurrency();;
            float investorPercentIncrease = (investorUpgradeAmount.Value * 0.01f) + 1f;
            mls.LogInfo($"Money being multiplied by {investorPercentIncrease}.");
            SemiFunc.StatSetRunCurrency((int)Math.Round(preInvestment * investorPercentIncrease));
            mls.LogInfo($"Previous money was {preInvestment}. New total is {SemiFunc.StatGetRunCurrency()}.");
            investorTriggered = true;
        }

        public static int CalculateInvestmentLevel(int totalLevel = 0)
        {
            foreach (PlayerAvatar playerAvatar in SemiFunc.PlayerGetAll())
            {
                if (Upgrades.GetUpgrade("Investor").GetLevel(playerAvatar) > 0)
                {
                    totalLevel += Upgrades.GetUpgrade("Investor").GetLevel(playerAvatar);
                }
            }
            mls.LogDebug($"Total Investment Level is {totalLevel}.");
            return totalLevel;
        }

        private static void InitExtraLifeUpgrade(PlayerAvatar player, int level)
        {
            if (!SemiFunc.IsMasterClientOrSingleplayer())
            {
                return;
            }
            if (Upgrades.GetUpgrade("LifeInsurance").GetLevel(player) > 0)
            {
                if (!SemiFunc.RunIsLevel())
                {
                    return;
                }
                mls.LogDebug($"{player.playerName} has life insurance!");
                // player.gameObject.AddComponent<UpgradeManager.ExtraLifeManager>();
            }
        }

        private static void UseExtraLifeUpgrade(PlayerAvatar player, int level)
        {
            if (!SemiFunc.IsMasterClientOrSingleplayer())
            {
                return;
            }
            if (Upgrades.GetUpgrade("LifeInsurance").GetLevel(player) > 0)
            {
                if (!SemiFunc.RunIsLevel())
                {
                    return;
                }
                mls.LogDebug($"{player.playerName} has life insurance!");
                // player.gameObject.AddComponent<UpgradeManager.ExtraLifeManager>();
            }
        }

        private static void InitUnstableUpgrade(PlayerAvatar playerInit, int level)
        {
            foreach (PlayerAvatar player in SemiFunc.PlayerGetAll())
            {
                if (Upgrades.GetUpgrade("UnstableCore").GetLevel(player) > 0)
                {
                    if (!player.gameObject.GetComponent<UnstableCoreManager>())
                    {
                        player.gameObject.AddComponent<UnstableCoreManager>();
                        mls.LogDebug($"{player.playerName} is unstable!");
                    }
                }
            }
        }

        private static void UseUnstableUpgrade(PlayerAvatar playerUse, int level)
        {
            foreach (PlayerAvatar player in SemiFunc.PlayerGetAll())
            {
                if (Upgrades.GetUpgrade("UnstableCore").GetLevel(player) > 0)
                {
                    if (!player.gameObject.GetComponent<UnstableCoreManager>())
                    {
                        player.gameObject.AddComponent<UnstableCoreManager>();
                        mls.LogDebug($"{player.playerName} is unstable!");
                    }
                }
            }
        }
    }
}
