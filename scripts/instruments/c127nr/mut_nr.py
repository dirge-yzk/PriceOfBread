"""(1249) c127nr 突然変異（㉚）：使い捨ての木（git archive）に変異を入れ、試験 NationalismUnification* を回して赤と緑を表にする（c124nr/mut_nr.py の写し・変異を A′ と K1 に合わせた）。
使い方: python mut_nr.py <コミット> <作業フォルダ（D:/…）> <出力の表> [変異の名,...]   （1 本ずつ・隔離ビルド・終わった木とビルドは消す）"""
import sys, os, subprocess, shutil, re
C, WK, OUT = sys.argv[1], sys.argv[2], sys.argv[3]
ONLY = set(sys.argv[4].split(',')) if len(sys.argv) > 4 else None
REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))))
IDEO, MNS, FRM, WLD, TST, INU = ('PriceOfBread.SimCore/Ideology.cs', 'PriceOfBread.SimCore/MultiNationSim.cs', 'PriceOfBread.SimCore/Formations.cs',
                                 'PriceOfBread.SimCore/World.cs', 'PriceOfBread.Tests/NationalismUnificationTests.cs', 'PriceOfBread.SimCore/Instruments/InstrumentsUnif.cs')
A1 = 'foreach (var kv in own) if (kv.Value > largest.GetValueOrDefault(kv.Key)) largest[kv.Key] = kv.Value;   // (1249) 形 A′：主の文化を問わない'
FL = 'foreach (var kv in total) f[kv.Key] = 1.0 - largest.GetValueOrDefault(kv.Key) / kv.Value;'
MUTS = [
 ('mu0', '2 本の旗を試験の中で明示しない（既定 0）', [
    (TST, 'private static void SetFlag(World w, double v) => w.Coef.NationalismFromDispersion = v;', 'private static void SetFlag(World w, double v) { }'),
    (TST, 'UnificationNatioShare = new Share(0.0), UnificationWarWeary = flag };', 'UnificationNatioShare = new Share(0.0), UnificationWarWeary = 0 };'),
    (TST, 'rhine.Nation!.Coef.UnificationWarWeary = flag;', 'rhine.Nation!.Coef.UnificationWarWeary = 0;')]),
 ('mu1', 'M_X を主の文化が X の国に限る（形 A へ戻す）', [
    (IDEO, A1, 'string main = w.SelfPower?.Culture ?? ""; if (main.Length > 0 && own.TryGetValue(main, out double m) && m > largest.GetValueOrDefault(main)) largest[main] = m;')]),
 ('mu2', 'M_X を世界の和にする（f＝0）', [(IDEO, FL, 'foreach (var kv in total) f[kv.Key] = 1.0 - kv.Value / kv.Value;')]),
 ('mu3', 'f を 1−f にする', [(IDEO, FL, 'foreach (var kv in total) f[kv.Key] = largest.GetValueOrDefault(kv.Key) / kv.Value;')]),
 ('mu4', '厭戦の行を外す', [
    (FRM, ': c.UnificationWarWeary > 0 && p.WarExhaustion > c.WarWearyThreshold ? UnificationBlock.Weary', ': false ? UnificationBlock.Weary')]),
 ('mu5', '標的の厭戦を読む', [
    (FRM, 'p.WarExhaustion > c.WarWearyThreshold ? UnificationBlock.Weary', 't.WarExhaustion > c.WarWearyThreshold ? UnificationBlock.Weary')]),
 ('mu6', '> を >= に', [
    (FRM, 'p.WarExhaustion > c.WarWearyThreshold ? UnificationBlock.Weary', 'p.WarExhaustion >= c.WarWearyThreshold ? UnificationBlock.Weary')]),
 ('mu7', '締めの後に表を消さない', [
    (MNS, 'if (splitWorlds is not null) Ideologies.ClearCultureSplit(splitWorlds);', 'if (splitWorlds is null) Ideologies.ClearCultureSplit(new List<World>());')]),
 ('mu8', 'プレイヤーの道が厭戦の旗を読まない', [
    (WLD, 'Formations.UnificationTargets(d, sp, rule), target, Coef) switch', 'Formations.UnificationTargets(d, sp, rule), target, new Coefficients { UnificationWarWeary = 0 }) switch')]),
 ('mu9', '表に無い札の既定を 1 へ', [(IDEO, 'fv >= 0 && fv <= 1 ? fv : 0; }', 'fv >= 0 && fv <= 1 ? fv : 1; }')]),
 ('mu10', '自由主義から引かずに足すだけ', [(IDEO, 'r[2] += lib * (1.0 - fCult);', 'r[2] += lib;')]),
 ('mu11', '計器 tensionwhy_s の理由の列を 1 本落とす', [
    (INU, "foreach (var v in vals) sb.Append('\\t').Append(FR(v));", "for (int k2 = 1; k2 < vals.Length; k2++) sb.Append('\\t').Append(FR(vals[k2]));")]),
 ('mu12', '表を作る行を外す', [(MNS, 'if (Ideologies.BuildCultureSplit(ws)) splitWorlds = ws;', 'if (ws.Count < 0) splitWorlds = ws;')]),
]
env = dict(os.environ, TEMP=WK + '/tmp', TMP=WK + '/tmp', AVALONIA_TELEMETRY_OPTOUT='1')
os.makedirs(WK + '/tmp', exist_ok=True)
rows = ['変異\t内容\tビルド\t赤の試験\t緑の試験']
pat = re.compile(r'失敗 PriceOfBread\.Tests\.(NationalismUnification\w*Tests)\.(\w+)')
for key, desc, edits in MUTS:
    if ONLY and key not in ONLY: continue
    tree, art = f'{WK}/t_{key}', f'{WK}/a_{key}'
    shutil.rmtree(tree, ignore_errors=True); os.makedirs(tree)
    subprocess.run(f'git -C "{REPO}" archive {C} | tar -x -C "{tree}"', shell=True)
    bad = None
    for f, old, new in edits:
        p = os.path.join(tree, f); b = open(p, 'rb').read()
        o, n = old.encode('utf-8'), new.encode('utf-8')
        if b.count(o) != 1: bad = f'置き場が {b.count(o)} 回（{f}）'; break
        open(p, 'wb').write(b.replace(o, n))
    if bad: rows.append(f'{key}\t{desc}\t{bad}\t-\t-'); shutil.rmtree(tree, ignore_errors=True); continue
    bd = subprocess.run(['dotnet', 'build', 'PriceOfBread.Tests', '-c', 'Release', '--artifacts-path', art, '-v', 'q', '-nologo'], cwd=tree, env=env, capture_output=True, text=True, encoding='utf-8', errors='replace')
    if bd.returncode != 0: rows.append(f'{key}\t{desc}\tBUILDFAIL\t-\t-'); continue
    t = subprocess.run(['dotnet', 'test', 'PriceOfBread.Tests', '-c', 'Release', '--artifacts-path', art, '--no-build', '--filter', 'FullyQualifiedName~NationalismUnification'],
                       cwd=tree, env=env, capture_output=True, text=True, encoding='utf-8', errors='replace')
    open(f'{WK}/test_{key}.txt', 'w', encoding='utf-8').write(t.stdout + t.stderr)
    red = sorted(set(m.group(2).split('_')[0] for m in pat.finditer(t.stdout)))
    tot = re.findall(r'合計: (\d+)', t.stdout)
    rows.append(f"{key}\t{desc}\tok（合計 {tot[-1] if tot else '?'}）\t{','.join(red) or '（無し）'}\t-")
    shutil.rmtree(tree, ignore_errors=True); shutil.rmtree(art, ignore_errors=True)
open(OUT, 'w', encoding='utf-8', newline='\n').write('\n'.join(rows) + '\n')
print('MUTDONE')
