# (947) 合流：読む線 c83cc（r942_work）と設計の材料 c83hs（r946_work）、恒等の線 c83tf の生（c83tf_work）を写す（copy940.py の型・バイトで読み書き・題の行に状態・本文は変えない・反証役の訂正を末尾に）。
#   写した text は CRLF を LF にする（新しいファイルは LF）。大きな写し（.cs の写し・ビルドの記録）は写さない。
import os, shutil, subprocess
T = 'D:/PoB_work/wt_m947'
D = T + '/docs/202611/20261107'
H = os.path.dirname(os.path.abspath(__file__))

def lf(b):
    return b.replace(b'\r\n', b'\n')

def doc(src, dst, head, fix):
    b = lf(open(src, 'rb').read())
    nl = b.index(b'\n'); first = b[:nl].decode('utf-8')
    assert first.startswith('# '), first[:20]
    rest = first[2:]
    if rest.startswith('【'):
        rest = rest[rest.index('】') + 1:]
    new = ('# ' + head + rest).encode('utf-8') + b[nl:]
    if not new.endswith(b'\n'):
        new += b'\n'
    new += lf(open(fix, 'rb').read())
    open(dst, 'wb').write(new)
    print('doc', os.path.basename(dst), len(new))

def cp(p, d):
    os.makedirs(d, exist_ok=True)
    open(os.path.join(d, os.path.basename(p)), 'wb').write(lf(open(p, 'rb').read()))
    print('cp', os.path.basename(p), '->', d.split('/')[-1])

SKIP_EXT = ('.cs', '.md')
def files(src, sdir, mdir, skip=()):
    for f in sorted(os.listdir(src)):
        p = os.path.join(src, f)
        if os.path.isdir(p) or f.endswith(SKIP_EXT) or '.mark' in f or f in skip or f.startswith('build_'):
            continue
        cp(p, sdir if (f.endswith('.py') or f.endswith('.sh')) else mdir)

os.makedirs(D, exist_ok=True)
# c83cc（読む線）
doc('D:/PoB_work/r942_work/20261107_建設の閉じの計器の穴_A-2の旗1_読み_下書き.md',
    D + '/20261107_建設の閉じの計器の穴_A-2の旗1_読み_下書き.md',
    '【読みの下書き・AI の案・作者の確認待ち】', H + '/fix942.md')
files('D:/PoB_work/r942_work', T + '/scripts/instruments/r942', T + '/measurements/m942_20261107')
cp('D:/PoB_work/r942_work/verdict.mark', T + '/measurements/m942_20261107')
for f in sorted(os.listdir('D:/PoB_work/r942_work/gate')):
    if not f.startswith('build_'):
        cp('D:/PoB_work/r942_work/gate/' + f, T + '/measurements/m942_20261107/gate')

# c83hs（material.md・draft.md は日付つきの 2 本と同じ中身＝日付つきを写す）
for kind, name in (('材料', 'material.md'), ('下書き', 'draft.md')):
    src = 'D:/PoB_work/r946_work/20261107_割譲の支配度の残り_開幕の州の支配度の初期値_盤面m933_%s.md' % kind
    assert open(src, 'rb').read() == open('D:/PoB_work/r946_work/' + name, 'rb').read()
    doc(src, D + '/' + os.path.basename(src), '【設計の材料と下書き・AI の案・確証なし・作者の確認待ち】', H + '/fix946.md')
files('D:/PoB_work/r946_work', T + '/scripts/instruments/r946', T + '/measurements/m946_20261107', skip=('material.md', 'draft.md'))
cp('D:/PoB_work/r946_work/table29.md', T + '/measurements/m946_20261107')

# c83tf の生（反証役：生と台本と突然変異の差分を measurements へ）
M = T + '/measurements/m941_20261107_tf'
for f in ('t0.txt', 't1.txt', 'g0.txt', 'g1.txt', 'm1_0.txt', 'm1_1.txt', 'cnt_wt1.txt', 'cnt_wt_c83tf.txt', 'full_nonfull.txt', 'full2.mark.log'):
    cp('D:/PoB_work/c83tf_work/' + f, M)
cp('D:/PoB_work/c83tf_work/fix.py', T + '/scripts/instruments/c83tf')
for w in ('mut0', 'mut1', 'wt1'):
    d = subprocess.run(['git', '-C', 'D:/PoB_work/c83tf_work/' + w, 'diff'], capture_output=True).stdout
    open(M + '/diff_%s.txt' % w, 'wb').write(lf(d)); print('diff', w, len(d))
