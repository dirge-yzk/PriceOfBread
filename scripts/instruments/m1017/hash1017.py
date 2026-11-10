# (1017) 合流：生のフォルダの表ごとの sha1 を書く（線の生を消す前に取り、合わせた木の腕の生と突き合わせる・読むだけ）。
#   python -X utf8 -I hash1017.py <ins フォルダ> <出力 tsv>
#   python -X utf8 -I hash1017.py --cmp <a.tsv> <b.tsv> [揺れの表の頭,...]   ＝共通の表の一致・違い・片側だけを数える
import sys, os, hashlib
if sys.argv[1] == '--cmp':
    A = dict(l.rstrip('\n').split('\t') for l in open(sys.argv[2], encoding='utf-8'))
    B = dict(l.rstrip('\n').split('\t') for l in open(sys.argv[3], encoding='utf-8'))
    noise = [x for x in (sys.argv[4].split(',') if len(sys.argv) > 4 else []) if x]
    same = [k for k in A if k in B and A[k] == B[k]]; diff = [k for k in A if k in B and A[k] != B[k]]
    dn = [k for k in diff if any(k.startswith(n) for n in noise)]; dx = [k for k in diff if k not in dn]
    onlyA = sorted(k for k in A if k not in B); onlyB = sorted(k for k in B if k not in A)
    print(f'共通 {len(same) + len(diff)}・一致 {len(same)}・揺れの表の違い {len(dn)}・それ以外の違い {len(dx)}・a だけ {len(onlyA)}・b だけ {len(onlyB)}')
    for k in dx[:40]: print('違い', k)
    print('a だけ（頭）', sorted({k.rsplit('_s', 1)[0] for k in onlyA}))
    print('b だけ（頭）', sorted({k.rsplit('_s', 1)[0] for k in onlyB}))
    sys.exit(0)
D, O = sys.argv[1], sys.argv[2]
with open(O, 'w', encoding='utf-8', newline='\n') as f:
    for n in sorted(os.listdir(D)):
        p = os.path.join(D, n)
        if not os.path.isfile(p): continue
        h = hashlib.sha1()
        with open(p, 'rb') as g:
            for c in iter(lambda: g.read(1 << 22), b''): h.update(c)
        f.write(n + '\t' + h.hexdigest() + '\n')
print('ok', O)
