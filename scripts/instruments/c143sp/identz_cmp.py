# (1357) c143sp 腕 Z の近似（identz_s1.sh）の比べ：素の 5 種・計器つき 10 種の成績表・.cost・計器の表のバイトを基準 m1355 と突き合わせる。
#   python -X utf8 identz_cmp.py <腕 Z の作業フォルダ> <リポジトリの measurements> <基準の生 keep_1355/base_ins>
#   成績表は名乗り行（SOURCE-HASH・腕の名を含む行）を違いとして並べるだけ＝その他の行の違いの数を数える。表は stproc_s（足した計器）を除く。
import os, sys
W, M, RAW = sys.argv[1], sys.argv[2], sys.argv[3]


def lines(p):
    with open(p, 'rb') as f:
        return f.read().split(b'\n')


def cmp_report(arm, base):
    a, b = lines(arm), lines(base)
    diff = [(i, x, y) for i, (x, y) in enumerate(zip(a, b)) if x != y]
    print(f'{os.path.basename(arm)} vs {os.path.basename(base)}: 行 {len(a)}/{len(b)}・違う行 {len(diff)}')
    for i, x, y in diff[:6]:
        print(f'  行 {i+1}: 腕 {x[:150].decode("utf-8","replace")}')
        print(f'         基 {y[:150].decode("utf-8","replace")}')
    return len(a) == len(b), diff


ok = True
for arm, base in [('b5_c143spZa_5.txt', 'b5_20261125_m1355'), ('b10_c143spZb_10.txt', 'b10_20261125_m1355_ins')]:
    same_len, diff = cmp_report(os.path.join(W, arm), os.path.join(M, base))
    hdr = [d for d in diff if b'SOURCE-HASH' in d[1]]
    rest = len(diff) - len(hdr)
    print(f'  名乗り行（SOURCE-HASH）{len(hdr)}・それ以外 {rest}')
    ok &= same_len and rest == 0
    ca, cb = os.path.join(W, arm + '.cost'), os.path.join(M, base + '.cost')
    if os.path.exists(ca) and os.path.exists(cb):
        _, cd = cmp_report(ca, cb)
        ok &= len(cd) == 0

ins = os.path.join(W, 'out_c143spZb_10', 'ins')
same = diffs = miss = added = 0
bad = []
for fn in sorted(os.listdir(ins)):
    if fn.startswith('stproc_s'):
        added += 1
        continue
    bp = os.path.join(RAW, fn)
    if not os.path.exists(bp):
        miss += 1
        bad.append('基準に無い ' + fn)
        continue
    with open(os.path.join(ins, fn), 'rb') as f1, open(bp, 'rb') as f2:
        if f1.read() == f2.read():
            same += 1
        else:
            diffs += 1
            bad.append('違う ' + fn)
print(f'計器の表：バイト一致 {same}・違う {diffs}・基準に無い {miss}・足した表 stproc_s {added}')
for x in bad[:20]:
    print('  ' + x)
ok &= diffs == 0 and miss == 0
print('判定（近似＝段 1 の表＋stproc_s・nationgood_s）: ' + ('恒等' if ok else '不一致'))
