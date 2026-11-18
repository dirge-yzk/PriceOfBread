"""(1216) c122nr 突然変異（㉚）：使い捨ての木（git archive）に 1 か所の変異を入れ、試験 NationalismDispersionTests を回して赤と緑を表にする。
使い方: python mut_nr.py <コミット> <作業フォルダ> <出力の表>   （1 本ずつ・隔離ビルド・終わった木とビルドは消す）"""
import sys, os, subprocess, shutil, re
C, WK, OUT = sys.argv[1], sys.argv[2], sys.argv[3]
REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))))
IDEO, MNS, TST = 'PriceOfBread.SimCore/Ideology.cs', 'PriceOfBread.SimCore/MultiNationSim.cs', 'PriceOfBread.Tests/NationalismDispersionTests.cs'
MUTS = [
 ('M0', '旗 0（試験の旗の明示を外す）', TST, 'private static void SetFlag(World w, double v) => w.Coef.NationalismFromDispersion = v;', 'private static void SetFlag(World w, double v) { }'),
 ('mu1', 'f を反転（主の文化なら own÷world・少数は 0）', IDEO, 'kv.Key == main ? 1.0 - kv.Value / total[kv.Key] : 1.0;', 'kv.Key == main ? kv.Value / total[kv.Key] : 0.0;'),
 ('mu2', 'UpdateShares で f＝0 に固定', IDEO, 'fCult = split.TryGetValue(rg.Culture, out double fv) && fv >= 0 && fv <= 1 ? fv : 0;', 'fCult = 0;'),
 ('mu3', '自由主義から引かずに足すだけ', IDEO, 'r[2] += lib * (1.0 - fCult);', 'r[2] += lib;'),
 ('mu4', '国の主の文化で f を引く', IDEO, 'split.TryGetValue(rg.Culture, out double fv)', 'split.TryGetValue(w.SelfPower?.Culture ?? "", out double fv)'),
 ('mu5', '表を作る行を外す', MNS, 'if (Ideologies.BuildCultureSplit(ws)) splitWorlds = ws;', 'if (ws.Count < 0) splitWorlds = ws;'),
 ('mu6', '締めの後に表を消さない', MNS, 'if (splitWorlds is not null) Ideologies.ClearCultureSplit(splitWorlds);', 'if (splitWorlds is null) Ideologies.ClearCultureSplit(new List<World>());'),
]
env = dict(os.environ, TEMP=WK + '/tmp', TMP=WK + '/tmp', AVALONIA_TELEMETRY_OPTOUT='1')
os.makedirs(WK + '/tmp', exist_ok=True)
rows = ['変異\t内容\tビルド\t赤の試験\t緑の試験']
for key, desc, f, old, new in MUTS:
    tree, art = f'{WK}/t_{key}', f'{WK}/a_{key}'
    shutil.rmtree(tree, ignore_errors=True); os.makedirs(tree)
    a = subprocess.run(f'git -C "{REPO}" archive {C} | tar -x -C "{tree}"', shell=True)
    p = os.path.join(tree, f); b = open(p, 'rb').read()
    o, n = old.encode('utf-8'), new.encode('utf-8')
    if b.count(o) != 1: rows.append(f'{key}\t{desc}\t変異の置き場が {b.count(o)} 回\t-\t-'); continue
    open(p, 'wb').write(b.replace(o, n))
    bd = subprocess.run(['dotnet', 'build', 'PriceOfBread.Tests', '-c', 'Release', '--artifacts-path', art, '-v', 'q', '-nologo'], cwd=tree, env=env, capture_output=True, text=True, encoding='utf-8', errors='replace')
    if bd.returncode != 0: rows.append(f'{key}\t{desc}\tBUILDFAIL\t-\t-'); continue
    t = subprocess.run(['dotnet', 'test', 'PriceOfBread.Tests', '-c', 'Release', '--artifacts-path', art, '--no-build', '--filter', 'FullyQualifiedName~NationalismDispersionTests'], cwd=tree, env=env, capture_output=True, text=True, encoding='utf-8', errors='replace')
    open(f'{WK}/test_{key}.txt', 'w', encoding='utf-8').write(t.stdout + t.stderr)
    red = sorted(set(re.findall(r'失敗 PriceOfBread\.Tests\.NationalismDispersionTests\.(\w+)', t.stdout)))
    allt = ['T1a_表のfは文化の人口の住む場所の割合', 'T1b_T1c_分断の国のPOPは民族主義へ移り_和は同じ', 'T1d_少数の州のPOPは州の文化でfを引く', 'T2_fが0の国は旗1でも取り分が同じ', 'T3_実の経路_四半期の末に表を作り締めの後に消す', 'T4_四半期の途中のセーブの後も同じ表が導ける']
    short = lambda s: s.split('_')[0] if not s.startswith('T1b') else 'T1b_T1c'
    rows.append(f"{key}\t{desc}\tok\t{','.join(short(x) for x in red) or '（無し）'}\t{','.join(short(x) for x in allt if x not in red)}")
    shutil.rmtree(tree, ignore_errors=True); shutil.rmtree(art, ignore_errors=True)
open(OUT, 'w', encoding='utf-8', newline='\n').write('\n'.join(rows) + '\n')
print('MUTDONE')
