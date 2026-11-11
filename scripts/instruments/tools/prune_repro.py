# (1054) (1053) の先頭の列 30/30 を、生を読まずに hash/ の sha1 から manifest の形で再現する。manifest の先頭の列を別の読み手の値と突き合わせる。
#   python -X utf8 -I prune_repro.py hash <hash フォルダ>
#       ＝ z.tsv・pre_z.tsv（腕）と base1044.tsv・pre_base.tsv（基準）の sha1 だけから、表ごとに〈消す／残す〉と先頭の列の一致を数える（生は読まない）。
#   python -X utf8 -I prune_repro.py manifest <prune_manifest.tsv> <参照の tsv＝表・列数・sha1>
#       ＝ prune.py の manifest の sha1_prefix_LF が、別の読み手（prefix1044.py を INSRAW に回した物）の値と全て一致するか。
import sys


def load3(p):
    d = {}
    for l in open(p, encoding='utf-8'):
        x = l.rstrip('\n').split('\t')
        if l.startswith('#') or len(x) < 2: continue
        d[x[0]] = x[1:]
    return d


def mode_hash(h):
    z, b = load3(h + '/z.tsv'), load3(h + '/base1044.tsv')
    pz, pb = load3(h + '/pre_z.tsv'), load3(h + '/pre_base.tsv')
    same = [n for n in z if n in b and z[n][0] == b[n][0]]
    diff = [n for n in z if n in b and z[n][0] != b[n][0]]
    ok = sum(1 for n in pb if n in pz and pz[n] == pb[n])
    print(f'全体の sha1：腕 {len(z)} 本・基準 {len(b)} 本・一致（刈る）{len(same)}・違い（残す）{len(diff)}')
    print('違いの表の頭：' + ','.join(sorted({n.rsplit('_s', 1)[0] for n in diff})))
    print(f'先頭の列：{ok}/{len(pb)}')
    return 0 if ok == len(pb) and len(pb) else 1


def mode_manifest(mp, ref):
    man = {}
    for l in open(mp, encoding='utf-8'):
        if l.startswith('#'): continue
        x = l.rstrip('\n').split('\t'); man[x[0]] = x
    ok = n = 0
    for name, v in load3(ref).items():
        n += 1; x = man.get(name)
        if x is not None and x[4] == v[0] and x[5] == v[1]: ok += 1
        else: print('違い', name)
    print(f'先頭の列：{ok}/{n}')
    return 0 if ok == n and n else 1


if __name__ == '__main__':
    sys.exit(mode_hash(sys.argv[2]) if sys.argv[1] == 'hash' else mode_manifest(sys.argv[2], sys.argv[3]))
