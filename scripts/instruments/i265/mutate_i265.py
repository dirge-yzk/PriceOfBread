"""(265) 番人の突然変異（読むだけの計器の番人が、わざと壊した計器で赤になるか）。
  python -X utf8 mutate_i265.py <ソース木(worktree)> <作業フォルダ> <結果ファイル>
  木を作業フォルダへ写し（bin/obj/docs/measurements を除く）、1つずつ書き換えてビルドし、InstrumentsTests だけを走らせて赤になった試験の名前を書く。
  書き換えは木の写しの中だけ（元の木は触らない）。"""
import os, re, shutil, subprocess, sys

SRC, WORK, RES = sys.argv[1], sys.argv[2], sys.argv[3]
TREE = os.path.join(WORK, 'mut_tree')
ART = os.path.join(WORK, 'art_mut')
ENV = dict(os.environ, AVALONIA_TELEMETRY_OPTOUT='1')

MUTS = [
    ('M1 行政の必要額で切る min を max にする', 'PriceOfBread.SimCore/Instruments.cs',
     'st.BudMinB += Math.Min(r.AdminBudget, nb);', 'st.BudMinB += Math.Max(r.AdminBudget, nb);'),
    ('M2 国債金利の議会の割引の項の符号を反転', 'PriceOfBread.SimCore/Fiscal.cs',
     'bondInside * convDiscount * (reprDiscount - 1.0), w.GovBondRatePerYear.Value - bondPre, dGdp.Value, convDiscount, reprDiscount);',
     '-bondInside * convDiscount * (reprDiscount - 1.0), w.GovBondRatePerYear.Value - bondPre, dGdp.Value, convDiscount, reprDiscount);'),
    ('M3 段5 の上限から前月の所得 E を落とす', 'PriceOfBread.SimCore/Instruments.cs',
     'return Math.Min(s, e + s * d);', 'return Math.Min(s, s * d);'),
    ('M4 大衆の定義から階級の条件を外す', 'PriceOfBread.SimCore/Instruments.cs',
     'static bool IsMass(Pop p) => !p.IsSoldier && (p.ClassId == PopClass.Peasant || p.ClassId == PopClass.Worker || p.ClassId == PopClass.Artisan);',
     'static bool IsMass(Pop p) => !p.IsSoldier;'),
    ('M5 州の割譲の記帳 RegionMoved の呼びを消す', 'PriceOfBread.SimCore/Territory.cs',
     'if (Instruments.On) Instruments.RegionMoved(to, r);', '// MUTATED: RegionMoved call removed'),
    ('M6 国営工場の売却の件数の呼びを消す', 'PriceOfBread.SimCore/World.cs',
     'if (Instruments.On) Instruments.FactorySold(this);', '// MUTATED: FactorySold call removed'),
    ('M7 反事実の借入′の min を max にする', 'PriceOfBread.SimCore/Instruments.cs',
     'double sp = spare0 + x, b2 = Math.Min(borrowed, Math.Max(0, -sp));', 'double sp = spare0 + x, b2 = Math.Max(borrowed, Math.Max(0, -sp));'),
    ('M8 残存国の述語の州数の上限を1にする', 'PriceOfBread.SimCore/Instruments.cs',
     'activeRegions <= 2 && popK >= 100', 'activeRegions <= 1 && popK >= 100'),
    ('M9 段5 の拘束の判定を Paid＞上限 から Paid≧上限 の反対向き（＜）へ', 'PriceOfBread.SimCore/Instruments.cs',
     'if (paid > cap) { st.QCapBoundPaid[k] += paid;', 'if (paid < cap) { st.QCapBoundPaid[k] += paid;'),
    ('M10 必要額の距離の割増を落とす', 'PriceOfBread.SimCore/Instruments.cs',
     '* Math.Max(r.Markets[Catalog.AdminId].Price, 1e-9) * Fiscal.AdminDistanceMul(w, r);', '* Math.Max(r.Markets[Catalog.AdminId].Price, 1e-9);'),
]


def sh(cmd, **kw):
    return subprocess.run(cmd, shell=True, capture_output=True, text=True, encoding='utf-8', errors='replace', env=ENV, **kw)


def main():
    if os.path.isdir(TREE):
        shutil.rmtree(TREE)
    shutil.copytree(SRC, TREE, ignore=shutil.ignore_patterns('bin', 'obj', '.git', 'docs', 'measurements', 'uishot', 'dist', '.vs'))
    out = []
    for name, rel, old, new in MUTS:
        p = os.path.join(TREE, rel)
        b = open(p, 'rb').read()
        s = b.decode('utf-8')
        if s.count(old) != 1:
            out.append(f'{name}: 書き換え対象が {s.count(old)} 箇所（1箇所でない）＝スキップ')
            continue
        open(p, 'wb').write(s.replace(old, new).encode('utf-8'))
        try:
            r = sh(f'dotnet build "{TREE}/PriceOfBread.Tests/PriceOfBread.Tests.csproj" -c Release --artifacts-path "{ART}" --disable-build-servers')
            if ' 0 エラー' not in r.stdout:
                out.append(f'{name}: ビルド失敗\n' + '\n'.join(l for l in r.stdout.splitlines() if 'error' in l)[:600])
                continue
            t = sh(f'dotnet test "{TREE}/PriceOfBread.Tests/PriceOfBread.Tests.csproj" -c Release --no-build --artifacts-path "{ART}" --filter "FullyQualifiedName~InstrumentsTests"')
            red = sorted(set(re.findall(r'失敗 PriceOfBread\.Tests\.InstrumentsTests\.(\S+)', t.stdout)))
            tail = [l for l in t.stdout.splitlines() if l.startswith('失敗!') or l.startswith('成功!')]
            out.append(f'{name}: 赤={red if red else "なし"} ／ {tail[-1] if tail else "?"}')
        finally:
            open(p, 'wb').write(b)   # 元へ戻す
        open(RES, 'w', encoding='utf-8').write('\n'.join(out) + '\n')
    open(RES, 'w', encoding='utf-8').write('\n'.join(out) + '\n')
    print('\n'.join(out))


main()
