# (835) c67id：係数の関係の番人（CoefficientRelationTests）と HabitTests の戻した Fact の突然変異（使い捨ての木だけに当てる）。
#   使い方：python -I -X utf8 mut835_unit.py <コミット> <作業フォルダ> <出力 txt>
#   git archive の複製 1 本に 1 つずつ当てて関係する試験だけを回し、赤の試験名と赤の行（スタックの〈ファイル:line N〉）を出力へ書く。変異を当てるたびに元のバイトへ戻す。
import os, re, shutil, subprocess, sys, tarfile, io

commit, work, out = sys.argv[1], sys.argv[2], sys.argv[3]
repo = 'D:/PoB_work/wt_c67id'
src = os.path.join(work, 'mut_unit_src').replace(chr(92), '/')
art = os.path.join(work, 'mut_unit_art').replace(chr(92), '/')
if os.path.exists(src):
    shutil.rmtree(src)
os.makedirs(src)
data = subprocess.run(['git', '-C', repo, 'archive', commit], capture_output=True, check=True).stdout
tarfile.open(fileobj=io.BytesIO(data)).extractall(src)

COEF = 'PriceOfBread.SimCore/Coefficients.cs'
HAB = 'PriceOfBread.SimCore/Habit.cs'
HT = 'PriceOfBread.Tests/HabitTests.cs'
F_REL = 'FullyQualifiedName~CoefficientRelationTests'
F_HAB = 'FullyQualifiedName~HabitTests'
HFACT = '在庫が蓋を超えていれば1か月の更新で蓋の0_2へ戻る'

MUTS = [
    ('M0 変異なしの対照', None, F_REL + '|' + F_HAB, []),
    ('Mu1 WhitePeaceScore の既定を 0.2 に（0.22 ＞ 0.2・関係を破る）', COEF, F_REL, [('public double WhitePeaceScore = 0.25;', 'public double WhitePeaceScore = 0.2;')]),
    ('Mu1b WarPushSpeed の既定を 0.2 に（0.32 ＞ 0.25・関係を破る）', COEF, F_REL, [('public double WarPushSpeed = 0.10;', 'public double WarPushSpeed = 0.2;')]),
    ('Mu1c WhitePeaceScore を 0.3 に（関係は保たれる＝緑のはず・値の固定を外した確かめ）', COEF, F_REL, [('public double WhitePeaceScore = 0.25;', 'public double WhitePeaceScore = 0.3;')]),
    ('Mu2a 戻した Fact の期待 0.2 を 0.3 に（試験の側を壊す）', HT, F_HAB, [('        Assert.Equal(0.2, r.OpiumAddiction, 12);\n    }\n', '        Assert.Equal(0.3, r.OpiumAddiction, 12);\n    }\n')]),
    ('Mu2b Habit.NextStock の上限を +∞ に（蓋を外す）', HAB, F_HAB, [('0, h.Cap, 0);   // (739) 群3', '0, double.PositiveInfinity, 0);   // (739) 群3')]),
]


def run(name, rel, flt, pairs):
    orig = None
    if rel:
        p = os.path.join(src, rel)
        orig = open(p, 'rb').read()
        b = orig
        for o, n in pairs:
            o = o.encode('utf-8'); n = n.encode('utf-8')
            assert b.count(o) == 1, (name, o)
            b = b.replace(o, n)
        open(p, 'wb').write(b)
    try:
        r = subprocess.run(['dotnet', 'test', os.path.join(src, 'PriceOfBread.Tests/PriceOfBread.Tests.csproj'), '-c', 'Release', '--artifacts-path', art,
                            '--disable-build-servers', '--filter', flt], capture_output=True, text=True, encoding='utf-8', errors='replace')
    finally:
        if orig is not None:
            open(os.path.join(src, rel), 'wb').write(orig)
    txt = r.stdout + r.stderr
    lines = [f'{name}', f'    rc={r.returncode}（0＝緑・1＝赤）']
    for ln in txt.split('\n'):
        s = ln.strip()
        if re.match(r'^(失敗|成功)!', s) or s.startswith('失敗 ') or re.search(r'\.cs:line \d+', s) or 'error CS' in s:
            lines.append('    ' + s[:260])
    return '\n'.join(lines)


with open(out, 'w', encoding='utf-8', newline='\n') as f:
    f.write(f'(835) c67id 突然変異の記録（コミット {commit}・複製の木に 1 本ずつ当てて関係する試験だけを回した・台本 scripts/instruments/c67id/mut835_unit.py）\n')
    for name, rel, flt, pairs in MUTS:
        f.write(run(name, rel, flt, pairs) + '\n')
        f.flush()
    f.write('MUTDONE\n')
print('done')
