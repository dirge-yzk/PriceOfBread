# (1028) 突然変異 M1〜M7 を使い捨ての木で 1 本ずつ当て、試験を回して赤の試験名を書く。使い方：python mutate.py <木> <出力>
import subprocess, sys, os
tree, out = sys.argv[1], sys.argv[2]
W = 'PriceOfBread.SimCore/InstrumentsWar.cs'; N = 'PriceOfBread.SimCore/InstrumentsNationgood.cs'
MUT = {
 'M1': (W, "nOk = land * (1 - b) - newNeed >= 0;", "nOk = land * (1 - b) - oldNeed >= 0;"),
 'M2': (W, "return need * (1 - tau * tau);", "return need;"),
 'M3': (N, "foreach (var r in w.Regions) foreach (var p in r.Producers) { int pg = p.GoodId; if (pg >= 0 && pg < r.Markets.Length && pg < outSum.Length) outSum[pg] += p.Output; }",
           "foreach (var r in w.Regions) for (int pg = 0; pg < r.Markets.Length && pg < outSum.Length; pg++) outSum[pg] += r.Markets[pg].LastProduced;"),
 'M4': (N, " || (ev.HaveSeen && !ev.Seen.Contains(r))", ""),
 'M5': (W, "if (gw.ExpansionWill < gw.InvadeWillCost(target)) acc.CrossWeakWill++;   // (1028)", "acc.CrossWeakWill++;   // (1028)"),
 'M6': (W, "        if (reason == \"弱小国が標的\")\n        {\n            if (gw.ExpansionWill < gw.InvadeWillCost(target)) acc.CrossWeakWill++;   // (1028)\n",
           "        if (gw.ExpansionWill < gw.InvadeWillCost(target)) acc.CrossWeakWill++;   // (1028)\n        if (reason == \"弱小国が標的\")\n        {\n"),
 'M0': (N, "static int NgEventCount", "static int NgEventCount"),
 'M7': (N, "{ if (from.Diplomacy is { } dip) NgEvs.GetValue(dip, _ => new NgEv()).Moved.Add(r); }", "{ }"),
}
only = sys.argv[3].split(',') if len(sys.argv) > 3 else list(MUT)
res = open(out, 'a', encoding='utf-8')
for k in only:
    f, o, n = MUT[k]; p = os.path.join(tree, f)
    b = open(p, 'rb').read().decode('utf-8'); assert b.count(o) == 1, (k, b.count(o))
    open(p, 'wb').write(b.replace(o, n).encode('utf-8'))
    try:
        r = subprocess.run(['dotnet', 'test', os.path.join(tree, 'PriceOfBread.Tests'), '-c', 'Release', '--artifacts-path', 'D:/PoB_work/c97ws_mutart',
                            '--filter', 'FullyQualifiedName~WarInstrumentTests|FullyQualifiedName~NationgoodOutputEventTests'], capture_output=True, text=True, encoding='utf-8', errors='replace')
        txt = r.stdout
        fails = [l.strip() for l in txt.split('\n') if l.strip().startswith(('失敗 ', 'Failed '))]
        summ = [l for l in txt.split('\n') if '合計:' in l or 'Total tests' in l]
        res.write(f'{k}: rc={r.returncode} 赤={len(fails)} {fails[:6]} {summ}\n'); res.flush()
    finally:
        open(p, 'wb').write(b.encode('utf-8'))
res.close()
