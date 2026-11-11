using System;
using System.Collections.Generic;
using System.Linq;
using PriceOfBread.SimCore;

namespace PriceOfBread.App;

/// <summary>新規ゲームの世界の組み立てのうち、画面の型（Avalonia）に触れない純粋な部分（試験が読める）。
/// 観戦＝画面は通常と同じで、選んだ国にも効用AIを差し、外交の IsPlayer をどの国にも立てない＝自己対戦（Runner）と同じ形の世界。</summary>
public static class SimFactory
{
    /// <summary>世界の種の既定（従来の固定値）。`--seed` を渡さない起動は全てこれ。</summary>
    public const ulong DefaultSeed = 1789UL;

    /// <summary>世界の組み立て（助走の前まで）。決定論：シードは既定 1789UL（`--seed` で替えられる）・エージェントの生成順は従来と同一。
    /// 通常＝プレイヤー国だけ NullAgent ＋ IsPlayer。観戦＝全国が UtilityAgent で IsPlayer は 1 つも立てない。</summary>
    public static MultiNationSim Make(NationConfig[] cfgs, int playerIndex, bool spectate, ulong seed = DefaultSeed)
    {
        var playerCfg = cfgs[playerIndex];
        var sim = new MultiNationSim(cfgs, seed,
            cfg => !spectate && ReferenceEquals(cfg, playerCfg) ? new NullAgent() : new UtilityAgent());
        if (!spectate) sim.Diplomacy.Powers[playerIndex].IsPlayer = true;   // 外交AIも自国を介入しない
        return sim;
    }

    /// <summary>助走済みの世界のキャッシュのファイル名。鍵＝SimCore の MVID ＋ playerIndex ＋ 助走長 ＋ 観戦かどうか（観戦の世界と通常の世界は別物＝混ぜない）＋種（既定の種は名前に出さない＝従来の鍵のまま）。</summary>
    public static string WarmCacheFileName(string mvid, int playerIndex, int warmupQuarters, bool spectate, ulong seed = DefaultSeed)
        => $"warm_{mvid}_{playerIndex}_{warmupQuarters}{(spectate ? "_obs" : "")}{(seed != DefaultSeed ? "_s" + seed.ToString(System.Globalization.CultureInfo.InvariantCulture) : "")}.pob";

    /// <summary>この世界は観戦か（どの国にも IsPlayer が立っていない）。セーブにも旗を足さない＝IsPlayer と操作者はシムごと往復する。</summary>
    public static bool IsSpectating(MultiNationSim sim) => !sim.Diplomacy.Powers.Any(p => p.IsPlayer);

    /// <summary>映している国が消えた後の付け直し先。まず消えた国の州（<paramref name="home"/>＝消える前に控えた州）を受け取った国（再併合の親）→無ければ生き残りで最大の国（名目GDP）。見つからなければ -1。</summary>
    public static int SuccessorIndex(MultiNationSim sim, Region? home)
    {
        if (home is not null)
            for (int i = 0; i < sim.Nations.Count; i++)
                if (sim.Nations[i].World.Regions.Contains(home)) return i;
        int best = -1; double bestV = double.NegativeInfinity;
        for (int i = 0; i < sim.Nations.Count; i++)
        {
            double v = sim.Nations[i].World.NominalGdpPerMonth.Value;
            if (v > bestV) { bestV = v; best = i; }
        }
        return best;
    }
}

/// <summary>`--uishot` の年の引数の読み。</summary>
public static class UiShotArgs
{
    /// <summary>「1790」または「1790,1800,1810」。昇順・重複なしに整える。空・数でない・1783 未満・9999 超が 1 つでもあれば null。</summary>
    public static int[]? ParseYears(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        var list = new List<int>();
        foreach (var part in s.Split(',', StringSplitOptions.TrimEntries))
        {
            if (!int.TryParse(part, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out int y) || y < 1783 || y > 9999) return null;
            list.Add(y);
        }
        return list.Distinct().OrderBy(y => y).ToArray();
    }
}
