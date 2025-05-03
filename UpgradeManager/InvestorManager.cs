using System;
using UnityEngine;
using REPOLib.Modules;
using static NikkiUpgrades.NikkiUpgradesPlugin;

namespace NikkiUpgrades.UpgradeManager
{
    public class InvestorManager : MonoBehaviour
    {
        private void Update()
        {
            if (!SemiFunc.IsMasterClientOrSingleplayer())
            {
                return;
            }
            if (!investorUpgradeEnabled.Value || !investorUsed)
            {
                return;
            }
            if (SemiFunc.RunIsShop() && investorTriggered)
            {
                investorTriggered = false;
                this.enabled = false;
                return;
            }
            if (!SemiFunc.RunIsLobby())
            {
                return;
            }
            if (!investorTriggered)
            {
                int preInvestment = SemiFunc.StatGetRunCurrency();
                int investmentLevel = CalculateInvestmentLevel();
                float investorPercentIncrease = (investorUpgradeAmount.Value * (investmentLevel * 0.01f)) + 1f;
                mls.LogInfo($"Money being multiplied by {investorPercentIncrease}.");
                SemiFunc.StatSetRunCurrency((int)Math.Round(preInvestment * investorPercentIncrease));
                mls.LogInfo($"Previous money was {preInvestment}. New total is {SemiFunc.StatGetRunCurrency()}.");
                investorTriggered = true;
            }
        }
    }
}
