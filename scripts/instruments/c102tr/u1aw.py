# (1073) 世界の U1a の物差し（貿易の挙動の線の狙いの門 W1 の材料・紙 fric.py の U1aW を門の形にした）
#   世界の U1a＝全ての国・役務を含む・Σ越境の輸出の額÷Σ作った額（その年の nationgood_s・額＝数量×国の priceMeanGold・priceMeanGold が正の行だけ）。
#   年 1870 と 1913。種ごとに出して種の中央値。ヌル腕の幅＝ヌル腕どうし（基線を含む）の最大÷最小。
# 使い方:
#   python -X utf8 -I u1aw.py 基線=<ins フォルダ> ヌル1=<ins フォルダ> ヌル2=<ins フォルダ> [腕=<ins フォルダ> ...] [--min 1.10]
#   最初の 3 つ＝ヌル腕（基線・n1・n2）。それ以降＝腕。--min を付けると腕ごとに〈新÷旧が各年で ≧min かつ ヌル腕の幅の 2 倍の外〉の合否を出す（終了コード 0＝全ての腕が合格・1＝不合格あり）。
#   腕が無ければ幅だけを出す（終了コード 0）。--min が既定で付くのは腕がある場合だけ（1.10）。
import os, re, statistics as st, sys

YEARS = ('1870', '1913')


def fl(x):
    try: return float(x)
    except Exception: return float('nan')


def seeds(d):
    return sorted(int(m.group(1)) for f in os.listdir(d) for m in [re.fullmatch(r'nationgood_s(\d+)\.tsv', f)] if m)


def u1aw_one(d, s):
    out = {y: [0.0, 0.0] for y in YEARS}   # 年 -> [作った額, 越境の輸出の額]
    with open(os.path.join(d, 'nationgood_s%d.tsv' % s), encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for ln in f:
            v = ln.rstrip('\n').split('\t')
            if v[0] not in YEARS: continue
            p = fl(v[ix['priceMeanGold']])
            if not (p > 0): continue
            out[v[0]][0] += fl(v[ix['producedQty']]) * p
            out[v[0]][1] += fl(v[ix['crossExportQty']]) * p
    return {y: (x / pw if pw > 0 else float('nan')) for y, (pw, x) in out.items()}


def median_by_year(d):
    per = [u1aw_one(d, s) for s in seeds(d)]
    return {y: st.median(p[y] for p in per if p[y] == p[y]) for y in YEARS}, len(per)


def main(argv):
    mn = 1.10; args = []
    i = 0
    while i < len(argv):
        if argv[i] == '--min': mn = float(argv[i + 1]); i += 2
        else: args.append(argv[i]); i += 1
    arms = [a.split('=', 1) for a in args]
    if len(arms) < 3: print('ヌル腕 3 本（基線・n1・n2）が要る'); return 2
    res = {n: median_by_year(d) for n, d in arms}
    base = arms[0][0]; nulls = [n for n, _ in arms[:3]]
    ok = True
    for y in YEARS:
        vals = [res[n][0][y] for n in nulls]
        width = max(vals) / min(vals)
        print('== %s（種の中央値）' % y)
        print('  ヌル腕 ' + '  '.join('%s %.5f' % (n, res[n][0][y]) for n in nulls) + '  ＝幅（最大÷最小）%.3f' % width)
        for n, _ in arms[3:]:
            r = res[n][0][y] / res[base][0][y]
            need = max(mn, 1.0 + 2.0 * (width - 1.0))
            good = r >= need
            ok = ok and good
            print('  腕 %s %.5f ＝基線比 %.3f（合格の線 %.3f＝max(--min, 幅の 2 倍の外)）%s' % (n, res[n][0][y], r, need, '合格' if good else '不合格'))
    print('種の数: ' + '  '.join('%s %d' % (n, res[n][1]) for n, _ in arms))
    return 0 if ok else 1


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
