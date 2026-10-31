# urb3 (706)：計器の生の突き合わせ（読むだけ）。基線のフォルダと腕のフォルダの同名の表をバイトで比べ、違う表は列ごとに違う列名を挙げる。
# 使い方：python -X utf8 -I scripts/instruments/urb3/cmp_ins.py <基線のフォルダ> <腕のフォルダ> [--known 列名,列名...]
#   --known に挙げた列（例：seconds・worldTotalPerMonth・sourceLine）だけが違う表は〈既知の揺れ〉として別に数える。
#   腕に基線に無い列が末尾に増えている表（urxpd_s の新しい 4 列など）は〈列が増えた〉として基線の列まで切って比べる。
import sys, os, csv, hashlib
sys.stdout.reconfigure(encoding='utf-8')
csv.field_size_limit(1 << 24)


def sha(p):
    h = hashlib.sha256()
    with open(p, 'rb') as f:
        for b in iter(lambda: f.read(1 << 20), b''):
            h.update(b)
    return h.hexdigest()


def cols_diff(pa, pb):
    """pa（基線）の列を基準に、pb で違う列名と、pb にだけある列を返す。行数が違うときは 'ROWS' を含める。"""
    with open(pa, encoding='utf-8', newline='') as fa, open(pb, encoding='utf-8', newline='') as fb:
        ra = csv.reader(fa, delimiter='\t'); rb = csv.reader(fb, delimiter='\t')
        ha = next(ra); hb = next(rb)
        extra = [c for c in hb if c not in ha]
        common = [c for c in ha if c in hb]
        ia = {c: ha.index(c) for c in common}; ib = {c: hb.index(c) for c in common}
        diff = set()
        if [c for c in hb if c in ha] != ha:
            diff.add('HEADER_ORDER_OR_MISSING')
        n = 0
        for xa, xb in zip(ra, rb):
            n += 1
            for c in common:
                if c in diff:
                    continue
                if ia[c] < len(xa) and ib[c] < len(xb) and xa[ia[c]] != xb[ib[c]]:
                    diff.add(c)
        restA = sum(1 for _ in ra); restB = sum(1 for _ in rb)
        if restA or restB:
            diff.add('ROWS')
    return sorted(diff), extra


def main():
    a, b = sys.argv[1], sys.argv[2]
    known = set()
    if '--known' in sys.argv:
        known = set(sys.argv[sys.argv.index('--known') + 1].split(','))
    fa = sorted(os.listdir(a)); fb = set(os.listdir(b))
    same = 0; missing = []; only_b = sorted(fb - set(fa)); known_only = []; real = []; grown = []
    for f in fa:
        if f not in fb:
            missing.append(f); continue
        pa, pb = os.path.join(a, f), os.path.join(b, f)
        if os.path.getsize(pa) == os.path.getsize(pb) and sha(pa) == sha(pb):
            same += 1; continue
        if not f.endswith('.tsv'):
            real.append((f, ['NONTSV'])); continue
        d, extra = cols_diff(pa, pb)
        if extra:
            grown.append((f, extra))
        if not d:
            if not extra:
                real.append((f, ['BYTES_ONLY']));
            continue
        if set(d) <= known:
            known_only.append((f, d))
        else:
            real.append((f, d))
    print(f"基線 {len(fa)} 本・腕 {len(fb)} 本")
    print(f"バイト一致 {same} 本")
    print(f"基線にあって腕に無い {len(missing)} 本：{missing[:10]}")
    print(f"腕にだけある {len(only_b)} 本：{only_b[:10]}")
    print(f"列が増えた表（基線の列は一致のものを含む）{len(grown)} 本")
    for f, e in grown[:5]:
        print(f"   {f}\t+{e}")
    print(f"既知の揺れの列だけが違う表 {len(known_only)} 本")
    for f, d in known_only[:40]:
        print(f"   {f}\t{d}")
    print(f"それ以外に違う表 {len(real)} 本")
    for f, d in real[:60]:
        print(f"   {f}\t{d}")


main()
