# (363) 合流が pa の mut.py を写して ROOT を合流の複製の木へ・M16・M17 を足した（反証役 pa〔v〕低#2・#3・#5）
import subprocess, sys, os
ROOT = 'D:/PoB_work/merge_363/mt'
P = ROOT + '/PriceOfBread.SimCore/InstrumentsPop.cs'
SIM = ROOT + '/PriceOfBread.SimCore/Simulation.cs'
INS = ROOT + '/PriceOfBread.SimCore/Instruments.cs'
MUT = [
 ('M1 公費を外す', P, '(1.0 - x.PublicShare)', '1.0'),
 ('M2 E から児童労働の割合を外す', P, 'x.YAdult * x.ChildWork * x.ChildLaborRate * (PopChildWorkYears * 12)', 'x.YAdult * x.ChildWork * (PopChildWorkYears * 12)'),
 ('M3 L60 の層 6 と 7 を入れ替え', P, 'return (1.0 - s) / mu6 + s / mu7;', 'return (1.0 - s) / mu7 + s / mu6;'),
 ('M4 平常の死の層別を一様の重みで', P, 'Pop.TakeWeighted(tmp, took, mortW, deaths);', 'Pop.TakeWeighted(tmp, took, ReadOnlySpan<double>.Empty, deaths);'),
 ('M5 飢えの死を数えない', SIM, 'Instruments.DemoFamine(World, p, d, disasterW);', '{ }'),
 ('M6 籠を読む順（CeFold の後）', INS, '            PopFold(sk, seed, w, st, q, picked, yearEnd);   // (361) 人口の表（籠は ce_s の積算を読む＝CeFold が戻す前）\n            CeFold(sk, seed, w, st, q, picked, yearEnd);   // (345) 消費の入口の表（階級の所得と支出を戻す前）',
  '            CeFold(sk, seed, w, st, q, picked, yearEnd);   // (345) 消費の入口の表（階級の所得と支出を戻す前）\n            PopFold(sk, seed, w, st, q, picked, yearEnd);'),
 ('M7 N 行を挙げた国だけに', P, 'if (!(picked || yearEnd)) return;', 'if (!picked) return;'),
 ('M8 出生を積まない', P, 'a.PopMonths += size0; a.Births += births;', 'a.PopMonths += size0;'),
 ('M9 月の数を数えない', P, 'var a = PAcc(w); a.Months++;', 'var a = PAcc(w);'),
 ('M10 s10 の 10 を落とす', P, 'double s10 = Math.Exp(-10.0 * mu[0]);', 'double s10 = Math.Exp(-1.0 * mu[0]);'),
 ('M11 老後の扶養の (1-π) を外す', P, 'x.L60 * 12 * x.Basket * (1.0 - x.Pi) / x.Ns', 'x.L60 * 12 * x.Basket / x.Ns'),
 ('M12 加齢の流出を引かない的な 0〜9 歳: 層 0 の人年を層 1 に', P, 'for (int b = 0; b < Pop.AgeBands; b++) a.BandExposure[b] += n0[2 * b] + n0[2 * b + 1];', 'for (int b = 0; b < Pop.AgeBands; b++) a.BandExposure[b] += n0[2 * ((b + 1) % Pop.AgeBands)] + n0[2 * ((b + 1) % Pop.AgeBands) + 1];'),
 ('M13 戦死の差分を取らない', P, 'dWar = w.WarDeadCumulative - a.Diag0War;', 'dWar = w.WarDeadCumulative;'),
 ('M14 新居型の A に子の籠を入れない', P, 'x.YAdult / (x.Basket * (1.0 + PopTheta * x.Ns))', 'x.YAdult / x.Basket'),
 ('M15 計器を読むだけでなくなる（hook が識字を書く）', P, 'a.PopMonths += size0; a.Births += births;', 'a.PopMonths += size0; a.Births += births; p.Literacy += 1e-9;'),
 ('M16 飢えの死の層別を一様の重みで（反証役 pa〔v〕低#2）', P, 'Pop.TakeWeighted(n, took, disasterW, d);', 'Pop.TakeWeighted(n, took, ReadOnlySpan<double>.Empty, d);'),
 ('M17 平常の死を 1% 数え過ぎる（反証役 pa〔v〕低#3）', P, 'a.PopMonths += size0; a.Births += births; a.DeathsNormal += deaths;', 'a.PopMonths += size0; a.Births += births; a.DeathsNormal += deaths * 1.01;'),
]
only = sys.argv[1:]
for lab, path, old, new in MUT:
    if only and not any(lab.startswith(o) for o in only): continue
    s = open(path, 'rb').read()
    t = s.decode('utf-8')
    if t.count(old) != 1:
        print(lab, 'SKIP(置換対象が', t.count(old), '個)'); continue
    open(path, 'wb').write(t.replace(old, new).encode('utf-8'))
    try:
        b = subprocess.run(['dotnet', 'build', ROOT + '/PriceOfBread.Tests', '-c', 'Release', '--artifacts-path', 'D:/PoB_work/merge_363/art_mt'], capture_output=True, text=True, encoding='utf-8', errors='replace')
        if b.returncode != 0:
            print(lab, 'BUILD FAIL', [l for l in b.stdout.splitlines() if 'error' in l][:2]); continue
        r = subprocess.run(['dotnet', 'test', ROOT + '/PriceOfBread.Tests', '-c', 'Release', '--artifacts-path', 'D:/PoB_work/merge_363/art_mt', '--no-build', '--filter', 'FullyQualifiedName~PopulationInstrumentTests'], capture_output=True, text=True, encoding='utf-8', errors='replace')
        tail = [l for l in r.stdout.splitlines() if '失敗' in l and '[' in l or l.startswith('成功') or l.startswith('失敗!')]
        print(lab, '=>', 'RED' if r.returncode != 0 else 'GREEN(生き残り)', tail[:3], flush=True)
    finally:
        open(path, 'wb').write(s)
