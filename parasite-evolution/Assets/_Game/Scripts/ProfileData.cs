using System;
using System.Collections.Generic;

namespace ParasiteEvolution
{
    public enum UpgradeKind { CaptureSpeed, Capacity, Speed, Radius, Channels, Health }

    [Serializable]
    public sealed class ProfileData
    {
        public string format;
        public int schemaVersion;
        public long revision;
        public int bank;
        public int biomass;
        public int cargoUnits;
        public int cargoValue;
        public int totalCaptures;
        public int totalDeposits;
        public int upgradesBought;
        public int questStage;
        public int visitedZones;
        public int wardenKills;
        public int deathSerial;
        public int[] upgrades;
        public List<string> abilities;
        public float health;
        public float x;
        public float z;
        public float pullRemaining;
        public float moveRemaining;
        public float damageRemaining;

        public static ProfileData Fresh()
        {
            return new ProfileData {
                format = "parasite-evolution", schemaVersion = 1,
                upgrades = new int[6], abilities = new List<string> { "AB-PULL" },
                health = 50, x = -22, z = -10, visitedZones = 1
            };
        }

        public void Validate()
        {
            if (format != "parasite-evolution" || schemaVersion != 1)
                throw new InvalidOperationException("Unsupported save format.");
            if (revision < 0 || bank < 0 || bank > Balance.ResourceLimit || biomass < 0 ||
                cargoUnits < 0 || cargoValue < 0 || cargoValue > Balance.ResourceLimit ||
                totalCaptures < 0 || totalDeposits < 0 || upgradesBought < 0 ||
                questStage < 0 || questStage > Balance.QuestRewards.Length ||
                deathSerial < 0 || wardenKills < 0 || visitedZones < 0 || visitedZones > 7 ||
                upgrades == null || upgrades.Length != 6 || abilities == null)
                throw new InvalidOperationException("Invalid profile data.");
            foreach (var n in upgrades)
                if (n < 0 || n > Balance.MaxUpgrade) throw new InvalidOperationException("Invalid upgrade.");
            var seen = new HashSet<string>();
            foreach (var id in abilities)
                if (!Balance.IsAbility(id) || !seen.Add(id)) throw new InvalidOperationException("Invalid ability.");
            foreach (var v in new[] { health, x, z, pullRemaining, moveRemaining, damageRemaining })
                if (float.IsNaN(v) || float.IsInfinity(v)) throw new InvalidOperationException("Invalid number.");
            if (health < 0 || health > Balance.MaxHealth(this) || cargoUnits > Balance.Capacity(this) ||
                pullRemaining < 0 || moveRemaining < 0 || damageRemaining < 0 ||
                Math.Abs(x) > 1000 || Math.Abs(z) > 1000)
                throw new InvalidOperationException("Invalid runtime snapshot.");
        }
    }

    // All numeric values are prototype tuning, not approved production balance.
    public static class Balance
    {
        public const int ResourceLimit = 1000000000;
        public const int MaxUpgrade = 20;
        public const int DeathPenaltyBps = 1000; // PROVISIONAL: 10%; owner chose only a partial loss.
        public const float PullCooldown = 4;
        public const float MoveCooldown = 7;
        public const float DamageCooldown = 13;
        public static readonly int[] QuestRewards = { 12, 18, 25, 35, 60 };
        public static readonly int[] QuestGoals = { 4, 2, 1, 3, 1 };
        public static bool IsAbility(string id) => id == "AB-PULL" || id == "AB-MOVE" || id == "AB-DAMAGE";
        public static int Capacity(ProfileData p) => 6 + p.upgrades[(int)UpgradeKind.Capacity] * 2;
        public static float MaxHealth(ProfileData p) => 50 + 10 * p.upgrades[(int)UpgradeKind.Health];
        public static float Speed(ProfileData p) => 4.5f + .25f * p.upgrades[(int)UpgradeKind.Speed];
        public static float Radius(ProfileData p) => 3.2f + .18f * p.upgrades[(int)UpgradeKind.Radius];
        public static int Channels(ProfileData p) => Math.Min(3, 1 + p.upgrades[(int)UpgradeKind.Channels]);
        public static int UpgradeLimit(UpgradeKind kind) => kind == UpgradeKind.Channels ? 2 : MaxUpgrade;
        public static float CaptureDamage(ProfileData p) => 12 + 3 * p.upgrades[(int)UpgradeKind.CaptureSpeed];
        public static int AbilityPrice(string id) => id == "AB-MOVE" ? 35 : id == "AB-DAMAGE" ? 80 : -1;
        public static int UpgradePrice(ProfileData p, UpgradeKind kind)
            => (int)Math.Min(ResourceLimit, Math.Ceiling((kind == UpgradeKind.Channels ? 30 : 8) *
                Math.Pow(1.55, p.upgrades[(int)kind])));
        public static int Level(ProfileData p)
        {
            var remaining = p.biomass; var level = 1;
            while (level < 100 && remaining >= level * 30) { remaining -= level * 30; level++; }
            return level;
        }
        public static int LevelProgress(ProfileData p)
        {
            var level = Level(p);
            return p.biomass - 30 * (level - 1) * level / 2;
        }
        public static int QuestProgress(ProfileData p)
        {
            switch (p.questStage)
            {
                case 0: return p.totalCaptures;
                case 1: return p.totalDeposits;
                case 2: return p.upgradesBought;
                case 3: return ((p.visitedZones & 1) != 0 ? 1 : 0) +
                               ((p.visitedZones & 2) != 0 ? 1 : 0) + ((p.visitedZones & 4) != 0 ? 1 : 0);
                case 4: return p.wardenKills;
                default: return 0;
            }
        }
    }

    public static class Economy
    {
        public static bool Capture(ProfileData p, int units, int value, int xp, bool warden)
        {
            if (units <= 0 || value < 0 || xp < 0 || units > Balance.Capacity(p) - p.cargoUnits ||
                (long)p.cargoValue + value > Balance.ResourceLimit ||
                (long)p.biomass + xp > Balance.ResourceLimit || p.totalCaptures == int.MaxValue ||
                (warden && p.wardenKills == int.MaxValue)) return false;
            p.cargoUnits += units; p.cargoValue += value; p.biomass += xp; p.totalCaptures++;
            if (warden) p.wardenKills++;
            return true;
        }
        public static bool Deposit(ProfileData p)
        {
            if (p.cargoUnits <= 0 || (long)p.bank + p.cargoValue > Balance.ResourceLimit ||
                p.totalDeposits == int.MaxValue) return false;
            p.bank += p.cargoValue; p.cargoUnits = 0; p.cargoValue = 0; p.totalDeposits++;
            return true;
        }
        public static bool Unlock(ProfileData p, string id)
        {
            var cost = Balance.AbilityPrice(id);
            if (cost < 0 || p.abilities.Contains(id) || p.bank < cost) return false;
            p.bank -= cost; p.abilities.Add(id); return true;
        }
        public static bool Upgrade(ProfileData p, UpgradeKind kind)
        {
            var index = (int)kind;
            if (index < 0 || index >= p.upgrades.Length || p.upgrades[index] >= Balance.UpgradeLimit(kind) ||
                ((kind == UpgradeKind.Channels || kind == UpgradeKind.Radius) && Balance.Level(p) < 2)) return false;
            var cost = Balance.UpgradePrice(p, kind);
            if (p.bank < cost) return false;
            p.bank -= cost; p.upgrades[index]++; p.upgradesBought++; return true;
        }
        public static bool ClaimQuest(ProfileData p)
        {
            var stage = p.questStage;
            if (stage >= Balance.QuestRewards.Length || Balance.QuestProgress(p) < Balance.QuestGoals[stage] ||
                (long)p.bank + Balance.QuestRewards[stage] > Balance.ResourceLimit) return false;
            p.bank += Balance.QuestRewards[stage]; p.questStage++; return true;
        }
        public static bool SettleDeath(ProfileData p, int serial, int penaltyBps)
        {
            if (serial <= p.deathSerial || serial != p.deathSerial + 1 || penaltyBps < 0 || penaltyBps > 10000)
                return false;
            p.bank -= (int)((long)p.bank * penaltyBps / 10000);
            p.deathSerial = serial;
            return true;
        }
    }
}
