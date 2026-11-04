# (836) 突然変異：使い捨ての木（D:/PoB_work/c67pu_mut）で 1 本ずつ入れ、CearnInstrumentTests を回して赤の試験を記録し、元へ戻す。
# 使い方：python mut836.py <木> <出力ファイル>
import subprocess, sys, re, os
T, OUT = sys.argv[1], sys.argv[2]
F = os.path.join(T, 'PriceOfBread.SimCore', 'InstrumentsCearn.cs')
MUTS = [
    ('M0', '何もしない（基準）', []),
    ('P1', 'POP の振り分けを入れ替える（行が次の POP の台帳を読む）',
     [(b'CgPops.TryGetValue(p, out cws[i]);', b'CgPops.TryGetValue(r.Pops[(i + 1) % n], out cws[i]);')]),
    ('P2', '人数 0 の POP を落とす（行を書かない）',
     [(b'var p = r.Pops[i]; var cw = cws[i]; int gi = CgGroupOf(p);', b'var p = r.Pops[i]; var cw = cws[i]; int gi = CgGroupOf(p); if (p.Size == 0) continue;')]),
    ('P3', '月初を月末で読む（POP の月の窓の始まり）',
     [(b'cw.QMonths++; cw.QMonthStart += cw.Begin;', b'cw.QMonths++; cw.QMonthStart += end;')]),
    ('M9', 'POP を群の台帳から落とす（農民の書き込みを群に足さない・cgrp_s）',
     [(b'            ga.Flow[(int)CearnCol.InMarket] += earned;\n            ga.Flow[(int)CearnCol.OutMarket] += earned - delta;',
       b'            if (p.ClassId != PopClass.Peasant) ga.Flow[(int)CearnCol.InMarket] += earned;\n            if (p.ClassId != PopClass.Peasant) ga.Flow[(int)CearnCol.OutMarket] += earned - delta;'),
      (b'        ga.Flow[(int)col] += isIn ? delta : -delta;', b'        if (p.ClassId != PopClass.Peasant) ga.Flow[(int)col] += isIn ? delta : -delta;')]),
]
orig = open(F, 'rb').read()
res = []
for key, desc, reps in MUTS:
    src = orig
    ok = True
    for a, b in reps:
        if src.count(a) != 1: ok = False; break
        src = src.replace(a, b)
    if not ok:
        res.append(f'{key} {desc}：置き換えの元が 1 箇所でない＝入れられない'); continue
    open(F, 'wb').write(src)
    b = subprocess.run(['dotnet', 'build', os.path.join(T, 'PriceOfBread.Tests', 'PriceOfBread.Tests.csproj'), '-c', 'Release', '--artifacts-path', 'D:/PoB_work/c67pu_mut_art', '--disable-build-servers'], capture_output=True, text=True, encoding='utf-8', errors='replace')
    if b.returncode != 0:
        res.append(f'{key} {desc}：ビルド失敗'); open(F, 'wb').write(orig); continue
    t = subprocess.run(['dotnet', 'test', os.path.join(T, 'PriceOfBread.Tests', 'PriceOfBread.Tests.csproj'), '-c', 'Release', '--no-build', '--artifacts-path', 'D:/PoB_work/c67pu_mut_art', '--filter', 'FullyQualifiedName~CearnInstrumentTests'], capture_output=True, text=True, encoding='utf-8', errors='replace')
    fails = sorted(set(m.split('.')[-1] for m in re.findall(r'失敗\s+(PriceOfBread\.Tests\.CearnInstrumentTests\.\S+)', t.stdout)))
    summ = [l.strip() for l in t.stdout.splitlines() if '合格' in l and '合計' in l]
    res.append(f'{key} {desc}：' + ('赤 ' + '・'.join(fails) if fails else '緑') + ' ／ ' + (summ[-1] if summ else '（集計の行なし）'))
    open(F, 'wb').write(orig)
open(OUT, 'w', encoding='utf-8', newline='\n').write('(836) 突然変異（木 ' + T + '・CearnInstrumentTests 11 本）\n' + '\n'.join(res) + '\n')
print('\n'.join(res))
