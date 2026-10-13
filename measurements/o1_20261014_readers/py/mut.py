"""(315) 突然変異：新しい番人 3 本が、壊したコードで赤くなることを確かめる。復元は元のバイトを書き戻して md5 で確認する。
使い方：python -X utf8 mut.py <worktree> <出力.txt>"""
import sys, os, subprocess, hashlib, shutil
WT = sys.argv[1]
OUT = sys.argv[2]
FIS = os.path.join(WT, 'PriceOfBread.SimCore', 'Fiscal.cs')
INS = os.path.join(WT, 'PriceOfBread.SimCore', 'Instruments.cs')
FILTER = 'FullyQualifiedName~第2条件|FullyQualifiedName~借入の門が第2条件で開いていても'
env = dict(os.environ)
env['PATH'] = r'C:\Program Files\dotnet;' + env.get('PATH', '')

MUTS = [
    ('M1 第2条件を常に偽', FIS, "(c.MaxInterestShareOfGdp.Value > 0 && w.LastInterestPerMonth < c.MaxInterestShareOfGdp * w.NominalGdpPerMonth)", "(false && w.LastInterestPerMonth < c.MaxInterestShareOfGdp * w.NominalGdpPerMonth)"),
    ('M2 未満を以下にする', FIS, "w.LastInterestPerMonth < c.MaxInterestShareOfGdp * w.NominalGdpPerMonth", "w.LastInterestPerMonth <= c.MaxInterestShareOfGdp * w.NominalGdpPerMonth"),
    ('M3 OR を AND にする', FIS, "w.LastInterestPerMonth < c.MaxInterestShareOfRevenue * w.LastRevenuePerMonth\n               || (c.MaxInterestShareOfGdp", "w.LastInterestPerMonth < c.MaxInterestShareOfRevenue * w.LastRevenuePerMonth\n               && (c.MaxInterestShareOfGdp"),
    ('M4 計数の門を借入の門と揃える', FIS, "bool underLimit = w.LastInterestPerMonth < c.MaxInterestShareOfRevenue * w.LastRevenuePerMonth;", "bool underLimit = BorrowGateOpen(w, c);"),
    ('M5 計器の旗を常に0', INS, "st.GdpOnlyNow = st.GateOpenNow == 1 && !(w.LastInterestPerMonth < w.Coef.MaxInterestShareOfRevenue * w.LastRevenuePerMonth) ? 1 : 0;", "st.GdpOnlyNow = 0;"),
    ('M6 計器の月数を二重に数える', INS, "if (st.GdpOnlyNow == 1) { st.QGdpOnlyMonths++;", "if (st.GdpOnlyNow == 1) { st.QGdpOnlyMonths += 2;"),
]
lines = []
for name, path, old, new in MUTS:
    orig = open(path, 'rb').read()
    md5 = hashlib.md5(orig).hexdigest()
    text = orig.decode('utf-8')
    if text.count(old) != 1:
        lines.append(f'{name}: 置換の対象が {text.count(old)} 件（中止）')
        continue
    open(path, 'wb').write(text.replace(old, new).encode('utf-8'))
    try:
        r = subprocess.run(['dotnet', 'test', os.path.join(WT, 'PriceOfBread.Tests'), '-c', 'Release', '--filter', FILTER], env=env, capture_output=True, text=True, encoding='utf-8', errors='replace')
        tail = [l for l in (r.stdout + r.stderr).splitlines() if '失敗' in l or 'Failed' in l or '成功!' in l or '失敗!' in l or 'error CS' in l]
        failed = [l.strip() for l in (r.stdout + r.stderr).splitlines() if l.strip().startswith('失敗 ') or l.strip().startswith('Failed ')]
        lines.append(f'{name}: exit={r.returncode} {" / ".join(tail[-2:])}')
        for l in failed[:6]:
            lines.append('    ' + l[:160])
    finally:
        open(path, 'wb').write(orig)
        ok = hashlib.md5(open(path, 'rb').read()).hexdigest() == md5
        lines.append(f'    復元 md5 一致: {ok}')
open(OUT, 'wb').write(('\n'.join(lines) + '\n').encode('utf-8'))
print('\n'.join(lines))
