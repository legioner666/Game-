using ParasiteEvolution;

internal static class Program
{
    private static int passed;
    private static int failed;

    private static int Main()
    {
        Run("fresh save is valid", () => ProfileData.Fresh().Validate());
        Run("capture respects cargo limit and is atomic", () =>
        {
            var p = ProfileData.Fresh();
            Check(Economy.Capture(p, 6, 18, 18, false), "initial capture");
            Check(!Economy.Capture(p, 1, 3, 3, false), "over-capacity capture rejected");
            Equal(18, p.biomass, "biomass");
            Equal(1, p.totalCaptures, "captures");
            p.Validate();
        });
        Run("deposit is not repeatable", () =>
        {
            var p = ProfileData.Fresh();
            Check(Economy.Capture(p, 2, 9, 6, false), "capture");
            Check(Economy.Deposit(p), "first deposit");
            Check(!Economy.Deposit(p), "second deposit rejected");
            Equal(9, p.bank, "bank");
            Equal(0, p.cargoUnits, "remaining cargo");
        });
        Run("unlock charges once and requires funds", () =>
        {
            var p = ProfileData.Fresh();
            p.bank = 34;
            Check(!Economy.Unlock(p, "AB-MOVE"), "insufficient funds");
            Equal(34, p.bank, "bank unchanged");
            p.bank = 100;
            Check(Economy.Unlock(p, "AB-MOVE"), "unlock");
            Check(!Economy.Unlock(p, "AB-MOVE"), "duplicate unlock");
            Check(!Economy.Unlock(p, "NOT-A-SKILL"), "unknown ability");
            Equal(65, p.bank, "bank charged once");
        });
        Run("channels have level gate and cap", () =>
        {
            var p = ProfileData.Fresh();
            p.bank = 1000;
            Check(!Economy.Upgrade(p, UpgradeKind.Channels), "gate level one");
            p.biomass = 30;
            Check(Economy.Upgrade(p, UpgradeKind.Channels), "upgrade one");
            Check(Economy.Upgrade(p, UpgradeKind.Channels), "upgrade two");
            var remaining = p.bank;
            Check(!Economy.Upgrade(p, UpgradeKind.Channels), "cap enforced");
            Equal(remaining, p.bank, "no extra charge");
            Equal(3, Balance.Channels(p), "channel cap");
        });
        Run("unknown upgrade kind is rejected", () =>
        {
            var p = ProfileData.Fresh();
            p.bank = 1000;
            Check(!Economy.Upgrade(p, (UpgradeKind)999), "invalid enum");
            Equal(1000, p.bank, "bank unchanged");
        });
        Run("death penalty rounds down and resists replay", () =>
        {
            var p = ProfileData.Fresh();
            p.bank = 97;
            Check(Economy.SettleDeath(p, 1, 1000), "first death");
            Equal(88, p.bank, "10 percent rounded down");
            Check(!Economy.SettleDeath(p, 1, 1000), "replay rejected");
            Equal(88, p.bank, "no double charge");
        });
        Run("death rejects skipped serial and out-of-range penalty", () =>
        {
            var p = ProfileData.Fresh();
            p.bank = 100;
            Check(!Economy.SettleDeath(p, 2, 1000), "skipped serial");
            Check(!Economy.SettleDeath(p, 1, 10001), "penalty above 100 percent");
            Check(!Economy.SettleDeath(p, 1, -1), "negative penalty");
            Equal(100, p.bank, "bank unchanged");
        });
        Run("quest reward is single-claim", () =>
        {
            var p = ProfileData.Fresh();
            p.totalCaptures = 4;
            Check(Economy.ClaimQuest(p), "quest one claim");
            Check(!Economy.ClaimQuest(p), "second claim rejected");
            Equal(1, p.questStage, "quest stage");
            Equal(12, p.bank, "reward");
        });
        Run("experience thresholds are stable", () =>
        {
            var p = ProfileData.Fresh();
            p.biomass = 29;
            Equal(1, Balance.Level(p), "level at 29");
            p.biomass = 30;
            Equal(2, Balance.Level(p), "level at 30");
            p.biomass = 90;
            Equal(3, Balance.Level(p), "level at 90");
            Equal(0, Balance.LevelProgress(p), "progress reset");
        });
        Run("failed deposit does not discard cargo", () =>
        {
            var p = ProfileData.Fresh();
            p.bank = Balance.ResourceLimit - 1;
            Check(Economy.Capture(p, 1, 3, 3, false), "capture");
            Check(!Economy.Deposit(p), "deposit would overflow");
            Equal(1, p.cargoUnits, "cargo preserved");
            Equal(Balance.ResourceLimit - 1, p.bank, "bank preserved");
        });
        Run("biomass overflow rejects capture atomically", () =>
        {
            var p = ProfileData.Fresh();
            p.biomass = Balance.ResourceLimit - 1;
            Check(!Economy.Capture(p, 1, 1, 2, false), "overflow rejected");
            Equal(0, p.cargoUnits, "cargo not added");
            Equal(0, p.totalCaptures, "capture counter");
        });
        Run("invalid saves are rejected", () =>
        {
            var p = ProfileData.Fresh();
            p.schemaVersion = 2;
            Throws<InvalidOperationException>(() => p.Validate(), "future schema");
            p = ProfileData.Fresh();
            p.health = float.NaN;
            Throws<InvalidOperationException>(() => p.Validate(), "NaN health");
        });

        Console.WriteLine($"Standalone economy checks: {passed} passed, {failed} failed.");
        return failed == 0 ? 0 : 1;
    }

    private static void Run(string name, Action test)
    {
        try
        {
            test();
            passed++;
            Console.WriteLine($"PASS {name}");
        }
        catch (Exception ex)
        {
            failed++;
            Console.Error.WriteLine($"FAIL {name}: {ex}");
        }
    }

    private static void Check(bool condition, string label)
    {
        if (!condition) throw new Exception(label);
    }

    private static void Equal<T>(T expected, T actual, string label)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new Exception($"{label}: expected {expected}, actual {actual}");
    }

    private static void Throws<T>(Action action, string label) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new Exception($"{label}: expected {typeof(T).Name}");
    }
}
