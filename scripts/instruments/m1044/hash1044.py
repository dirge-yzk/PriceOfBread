# (1044) 合流：生のフォルダの表ごとの sha1（全体）と、基準の生と見出しが違う表だけ〈基準の列の数までの先頭の列〉の sha1 を書く（読むだけ・m1017/hash1017.py の型＋先頭の列）。
#   python -X utf8 -I hash1044.py <ins フォルダ> <出力 tsv> [基準の ins フォルダ]
#   python -X utf8 -I hash1044.py --cmp <a.tsv> <b.tsv> [揺れの表の頭,...]   ＝全体の sha1 が違う表は、両方に先頭の列の sha1 があればそれで比べる
import sys, os, hashlib
if sys.argv[1] == '--cmp':
    def load(p):
        d = {}
        for l in open(p, encoding='utf-8'):
            x = l.rstrip('\n').split('\t'); d[x[0]] = x[1:]
        return d
    A, B = load(sys.argv[2]), load(sys.argv[3])
    noise = [x for x in (sys.argv[4].split(',') if len(sys.argv) > 4 else []) if x]
    same = pre = 0; dn = []; dx = []
    for k in A:
        if k not in B: continue
        if A[k][0] == B[k][0]: same += 1; continue
        a = A[k][0] if len(A[k]) < 3 or A[k][2] == '-' else A[k][2]
        b = B[k][0] if len(B[k]) < 3 or B[k][2] == '-' else B[k][2]
        if a == b: pre += 1; continue
        (dn if any(k.startswith(n) for n in noise) else dx).append(k)
    onlyA = sorted(k for k in A if k not in B); onlyB = sorted(k for k in B if k not in A)
    print(f'共通 {same + pre + len(dn) + len(dx)}・全体の一致 {same}・先頭の列で一致 {pre}・揺れの表の違い {len(dn)}・それ以外の違い {len(dx)}・a だけ {len(onlyA)}・b だけ {len(onlyB)}')
    for k in dx[:40]: print('違い', k)
    print('a だけ（頭）', sorted({k.rsplit('_s', 1)[0] for k in onlyA}))
    print('b だけ（頭）', sorted({k.rsplit('_s', 1)[0] for k in onlyB}))
    sys.exit(0)
D, O = sys.argv[1], sys.argv[2]; BASE = sys.argv[3] if len(sys.argv) > 3 else None
def head(p):
    with open(p, 'rb') as g: return g.readline()
with open(O, 'w', encoding='utf-8', newline='\n') as f:
    for n in sorted(os.listdir(D)):
        p = os.path.join(D, n)
        if not os.path.isfile(p): continue
        h = hashlib.sha1()
        with open(p, 'rb') as g:
            for c in iter(lambda: g.read(1 << 22), b''): h.update(c)
        k, hp = '-', '-'
        bp = os.path.join(BASE, n) if BASE else None
        if bp and os.path.isfile(bp):
            ha, hb = head(p).rstrip(b'\r\n').split(b'\t'), head(bp).rstrip(b'\r\n').split(b'\t')
            if ha != hb and ha[:len(hb)] == hb:
                k = len(hb); h2 = hashlib.sha1()
                with open(p, 'rb') as g:
                    for line in g: h2.update(b'\t'.join(line.rstrip(b'\r\n').split(b'\t')[:k]) + b'\n')
                hp = h2.hexdigest()
        f.write(f'{n}\t{h.hexdigest()}\t{k}\t{hp}\n')
print('ok', O)
