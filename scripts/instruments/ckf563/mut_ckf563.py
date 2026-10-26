# (563) 線 ckf の突然変異の台本：番人 DiscoveryEntranceTests（と休眠表）が各変異で赤になるか。
# 使い方：PYTHONUTF8=1 python mut_ckf563.py <worktree> <artifacts-path> <出力ログ>
#   各変異＝対象ファイルを退避→置換→dotnet test（フィルタ）→赤か緑かを記録→必ず元へ戻す（コミット前の状態を壊さない）。
# 期待＝M1〜M10 は全て赤（番人が殺す）。M11（計器 InstrumentsClock の呼びから altParent を外す）は番人が無く生き残る（自己申告の限界）。
import subprocess, sys, shutil, os

WT, ART, LOG = sys.argv[1], sys.argv[2], sys.argv[3]
FILTER = 'FullyQualifiedName~DiscoveryEntrance|FullyQualifiedName~DormantIdentity'
D = 'PriceOfBread.SimCore/Discovery.cs'
C = 'PriceOfBread.SimCore/Coefficients.cs'
K = 'PriceOfBread.SimCore/Catalog.cs'
I = 'PriceOfBread.SimCore/InstrumentsClock.cs'

MUTS = [
    ('M1 ParentEngineers が altParent を見ない', D, ': altParent && d.AltEngineerParent >= 0 ? d.AltEngineerParent', ': false && d.AltEngineerParent >= 0 ? d.AltEngineerParent'),
    ('M2 ParentEngineers が altParent を常に真にする', D, ': altParent && d.AltEngineerParent >= 0 ? d.AltEngineerParent', ': true && d.AltEngineerParent >= 0 ? d.AltEngineerParent'),
    ('M3 Score が旗 L を渡さない', D, 'poolOn && c.DiscoveryClockAltParent > 0) : double.NaN;', 'false) : double.NaN;'),
    ('M4 PriceBook が閉じた既定のレシピを使わない', D, 'if (!(best >= 0) && closedDefault is not null', 'if (false && closedDefault is not null'),
    ('M5 閉じた既定のレシピの表を旗に関わらず作る', D, 'c.DiscoveryPotentialPrice > 0 ? new Producer?[ng] : null', 'new Producer?[ng]'),
    ('M6 別名のレシピも閉じた表に入れ最後の物を採る', D, 'p.RecipeName.Length == 0 && p.SiteRow < 0 && p.RetiredByRow < 0 && closedDefault[p.GoodId] is null', 'p.SiteRow < 0 && p.RetiredByRow < 0'),
    ('M7 潜在の値段が既に値段のある財も上書きする', D, 'if (!(best >= 0) && closedDefault is not null', 'if (closedDefault is not null'),
    ('M8 旗 P の既定を 1 にする', C, 'public double DiscoveryPotentialPrice = 0;', 'public double DiscoveryPotentialPrice = 1;'),
    ('M9 旗 L の既定を 1 にする', C, 'public double DiscoveryClockAltParent = 0;', 'public double DiscoveryClockAltParent = 1;'),
    ('M10 機関車の行から別の親の列を外す', K, 'Replaces: LandFreight, AltEngineerParent: MachineryId),', 'Replaces: LandFreight),'),
    ('M11 計器 InstrumentsClock が旗 L を渡さない（番人なし＝生き残る見込み）', I, ', altParent: nations[i].World.Coef.DiscoveryClockAltParent > 0);', ');'),
]


def log(s):
    print(s, flush=True)
    with open(LOG, 'a', encoding='utf-8') as f:
        f.write(s + '\n')


for name, rel, old, new in MUTS:
    p = os.path.join(WT, rel)
    bak = p + '.mutbak'
    shutil.copyfile(p, bak)
    try:
        b = open(p, 'rb').read().decode('utf-8')
        if b.count(old) != 1:
            log(f'SKIP {name}: 置換元が {b.count(old)} 個')
            continue
        open(p, 'wb').write(b.replace(old, new).encode('utf-8'))
        r = subprocess.run(['dotnet', 'test', os.path.join(WT, 'PriceOfBread.Tests/PriceOfBread.Tests.csproj'), '-c', 'Release', '--artifacts-path', ART, '--filter', FILTER],
                           capture_output=True, text=True, encoding='utf-8', errors='replace')
        out = r.stdout + r.stderr
        built = 'エラー CS' not in out and 'error CS' not in out
        if not built:
            log(f'BUILDFAIL {name}')
        else:
            tail = [l for l in out.splitlines() if l.startswith('失敗!') or l.startswith('成功!')]
            log(('KILLED  ' if r.returncode != 0 else 'SURVIVED') + f' {name} | ' + (tail[-1] if tail else ''))
    finally:
        shutil.copyfile(bak, p)
        os.remove(bak)
log('DONE')
