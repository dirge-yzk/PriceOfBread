# (933) 合流：読む線 c81co（r931_work）と設計の材料 c81ld（r932_work）を写す（copy926.py の型・バイトで読み書き・題の行に状態・本文は変えない・反証役の訂正を末尾に足す）。
import os, shutil
T = 'D:/PoB_work/wt_m933'
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
        if os.path.isdir(p) or f.endswith('.md') or '.done' in f or 'mark_' in f:
            continue
        d = sdir if (f.endswith('.py') or f.endswith('.sh')) else mdir
        shutil.copyfile(p, os.path.join(d, f)); print('cp', f, '->', d.split('/')[-1])
    if sub:
        for f in sorted(os.listdir(sub)):
            shutil.copyfile(os.path.join(sub, f), os.path.join(sdir, f)); print('cp', f, '->', sdir.split('/')[-1])

# c81co（読む線）
doc('D:/PoB_work/r931_work/20261107_植民地格の州の産出0の続き_読み_下書き.md',
    D + '/20261107_植民地格の州の産出0の続き_読み_下書き.md',
    '【読みの下書き・AI の案・作者の確認待ち】', H + '/fix931.md')
files('D:/PoB_work/r931_work', T + '/scripts/instruments/r931', T + '/measurements/m931_20261107')

# c81ld（material.md・draft.md は日付つきの 2 本と同じ中身＝日付つきを写す）
for kind, name in (('材料', 'material.md'), ('下書き', 'draft.md')):
    src = 'D:/PoB_work/r932_work/20261107_地主の政治の導き直しの段C-0′＋D-0_盤面m919_%s.md' % kind
    assert open(src, 'rb').read() == open('D:/PoB_work/r932_work/' + name, 'rb').read()
    doc(src, D + '/' + os.path.basename(src), '【設計の材料と下書き・AI の案・作者の確認待ち】', H + '/fix932.md')
files('D:/PoB_work/r932_work', T + '/scripts/instruments/r932', T + '/measurements/m932_20261107', 'D:/PoB_work/r932_work/r932')
