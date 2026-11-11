# (940) 合流：読む線 c82fg（r938_work）と設計の材料 c82ln（r939_work）を写す（copy926.py の型・バイトで読み書き・題の行に状態・本文は変えない・反証役の訂正を末尾に足す）。
import os, shutil
T = 'D:/PoB_work/wt_m940'
D = T + '/docs/202611/20261107'
H = os.path.dirname(os.path.abspath(__file__))

def doc(src, dst, head, fix):
    b = open(src, 'rb').read()
    nl = b.index(b'\n'); first = b[:nl].decode('utf-8')
    assert first.startswith('# 【'), first[:20]
    rest = first[first.index('】') + 1:]
    new = ('# ' + head + rest).encode('utf-8') + b[nl:]
    if not new.endswith(b'\n'):
        new += b'\n'
    new += open(fix, 'rb').read()
    open(dst, 'wb').write(new)
    print('doc', os.path.basename(dst), len(new))

def files(src, sdir, mdir, sub=None):
    os.makedirs(sdir, exist_ok=True); os.makedirs(mdir, exist_ok=True)
    for f in sorted(os.listdir(src)):
        p = os.path.join(src, f)
        if os.path.isdir(p) or f.endswith('.md') or '.done' in f or 'mark_' in f or '.mark' in f:
            continue
        d = sdir if (f.endswith('.py') or f.endswith('.sh')) else mdir
        shutil.copyfile(p, os.path.join(d, f)); print('cp', f, '->', d.split('/')[-1])
    if sub:
        for f in sorted(os.listdir(sub)):
            shutil.copyfile(os.path.join(sub, f), os.path.join(sdir, f)); print('cp', f, '->', sdir.split('/')[-1])

# c82fg（読む線）
doc('D:/PoB_work/r938_work/20261107_反乱の側の国の建国からの名目GDP0_読み_下書き.md',
    D + '/20261107_反乱の側の国の建国からの名目GDP0_読み_下書き.md',
    '【読みの下書き・AI の案・作者の確認待ち】', H + '/fix938.md')
files('D:/PoB_work/r938_work', T + '/scripts/instruments/r938', T + '/measurements/m938_20261107')

# c82ln（material.md・draft.md は日付つきの 2 本と同じ中身＝日付つきを写す）
for kind, name in (('材料', 'material.md'), ('下書き', 'draft.md')):
    src = 'D:/PoB_work/r939_work/20261107_土地の持ち分の段B-2bと鉱山の地代の計器_盤面m933_%s.md' % kind
    assert open(src, 'rb').read() == open('D:/PoB_work/r939_work/' + name, 'rb').read()
    doc(src, D + '/' + os.path.basename(src), '【設計の材料と下書き・AI の案・作者の確認待ち】', H + '/fix939.md')
files('D:/PoB_work/r939_work', T + '/scripts/instruments/r939', T + '/measurements/m939_20261107')
