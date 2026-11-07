# (926) 合流：読む線 c80uk（r924_work）と設計の材料 c80dm（r925_work）を写す（バイトで読み書き・題の行に状態・本文は変えない・反証役の訂正を末尾に足す）
import os, shutil
T = 'D:/PoB_work/wt_m926'
D = T + '/docs/202611/20261107'

def doc(src, dst, title, fix):
    b = open(src, 'rb').read()
    nl = b.index(b'\n')
    first = b[:nl].decode('utf-8')
    assert first.startswith('# ')
    body = b[nl:]
    new = ('# ' + title + first[2:]).encode('utf-8') + body
    if not new.endswith(b'\n'):
        new += b'\n'
    new += open(fix, 'rb').read()
    open(dst, 'wb').write(new)
    print('doc', dst, len(new))

def files(src, sdir, mdir, skip):
    os.makedirs(sdir, exist_ok=True); os.makedirs(mdir, exist_ok=True)
    for f in sorted(os.listdir(src)):
        p = os.path.join(src, f)
        if f in skip or f.endswith('.md') or '.mark' in f:
            continue
        d = sdir if f.endswith('.py') else mdir
        shutil.copyfile(p, os.path.join(d, f))
        print('cp', f, '->', d.split('/')[-1])

# c80uk
doc('D:/PoB_work/r924_work/20261107_英の専制化の経路_読み_下書き.md',
    D + '/20261107_英の専制化の経路_読み_下書き.md',
    '【読みと設計の材料の下書き・AI の案・作者の確認待ち】', 'D:/PoB_work/m926/fix924.md')
files('D:/PoB_work/r924_work', T + '/scripts/instruments/r924', T + '/measurements/m924_20261107', set())

# c80dm（material.md・draft.md は日付つきの 2 本と同じ中身＝日付つきを写す）
for kind, name in (('材料', 'material.md'), ('下書き', 'draft.md')):
    src = 'D:/PoB_work/r925_work/20261107_割譲の支配度の次の段_獲得後の統治能力_Dominanceの引き継ぎ_盤面m919_%s.md' % kind
    assert open(src, 'rb').read() == open('D:/PoB_work/r925_work/' + name, 'rb').read()
    b = open(src, 'rb').read()
    nl = b.index(b'\n'); first = b[:nl].decode('utf-8')
    # 題の頭の【…】を〈設計の材料と下書き・AI の案・作者の確認待ち〉へ（題の残りは変えない）
    assert first.startswith('# 【')
    rest = first[first.index('】') + 1:]
    tmp = 'D:/PoB_work/m926/_t925.md'
    open(tmp, 'wb').write(('# ' + rest).encode('utf-8') + b[nl:])
    doc(tmp, D + '/' + os.path.basename(src), '【設計の材料と下書き・AI の案・作者の確認待ち】', 'D:/PoB_work/m926/fix925.md')
    os.remove(tmp)
files('D:/PoB_work/r925_work', T + '/scripts/instruments/r925', T + '/measurements/m925_20261107', set())
