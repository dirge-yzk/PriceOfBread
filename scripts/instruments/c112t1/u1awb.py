# (1121) 世界の U1a を〈今の値段〉(W1) と〈基線の値段〉(W1') で並べ、ヌル腕の幅と閾値 1+2*(幅-1) を出す。
#   W1  = Σ越境の輸出の数量×その腕の priceMeanGold ÷ Σ作った数量×その腕の priceMeanGold（c102tr/u1aw.py と同じ定義）
#   W1' = 同じ数量×基線の同じ種・同じ年・同じ国×財の priceMeanGold（基線で正の行だけ）
#   エンジンの関数は呼ばない。nationgood_s<seed>.tsv を自分で読む。
# 使い方: python -X utf8 -I u1awb.py 基線=<dir> N1=<dir> N2=<dir> [--mut own|dropnation]
#   --mut own        : 突然変異 U-M1（W1' に腕自身の値段を使う＝W1 と一致するはず）
#   --mut dropnation : 突然変異 U-M2（基線の値段の鍵から国を落とす＝国×財が混ざり値が変わるはず）
import os, re, statistics as st, sys

YEARS = ('1870', '1913')


def fl(x):
    try: return float(x)
    except Exception: return float('nan')


def seeds(d):
    return sorted(int(m.group(1)) for f in os.listdir(d) for m in [re.fullmatch(r'nationgood_s(\d+)\.tsv', f)] if m)


def rows(d, s):
    with open(os.path.join(d, 'nationgood_s%d.tsv' % s), encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for ln in f:
            v = ln.rstrip('\n').split('\t')
            if v[0] not in YEARS: continue
            yield v[0], v[ix['nation']], v[ix['good']], fl(v[ix['producedQty']]), fl(v[ix['crossExportQty']]), fl(v[ix['priceMeanGold']])


def key(y, n, gd, mut):
    return (y, gd) if mut == 'dropnation' else (y, n, gd)


def base_prices(d, s, mut):
    out = {}
    for y, n, gd, q, x, p in rows(d, s):
        out[key(y, n, gd, mut)] = p   # dropnation では同じ (年, 財) の最後の国の値段が残る＝突然変異
    return out


def one(d, s, bp, mut):
    w1 = {y: [0.0, 0.0] for y in YEARS}; w1b = {y: [0.0, 0.0] for y in YEARS}
    for y, n, gd, q, x, p in rows(d, s):
        if p > 0:
            w1[y][0] += q * p; w1[y][1] += x * p
        pb = p if mut == 'own' else bp.get(key(y, n, gd, mut), float('nan'))
        if pb > 0:
            w1b[y][0] += q * pb; w1b[y][1] += x * pb
    f = lambda t: {y: (t[y][1] / t[y][0] if t[y][0] > 0 else float('nan')) for y in YEARS}
    return f(w1), f(w1b)


def main(argv):
    mut = None; args = []
    i = 0
    while i < len(argv):
        if argv[i] == '--mut': mut = argv[i + 1]; i += 2
        else: args.append(argv[i]); i += 1
    arms = [a.split('=', 1) for a in args]
    if len(arms) != 3: print('ヌル腕 3 本（基線・N1・N2）が要る'); return 2
    bdir = arms[0][1]
    ss = seeds(bdir)
    for n, d in arms:
        if seeds(d) != ss: print('種の不一致', n, seeds(d)); return 2
    bps = {s: base_prices(bdir, s, mut) for s in ss}
    res = {}; per = {}
    for n, d in arms:
        per[n] = {s: one(d, s, bps[s], mut) for s in ss}
        res[n] = {k: {y: st.median(per[n][s][k][y] for s in ss) for y in YEARS} for k in (0, 1)}
    print('種:', ' '.join(map(str, ss)), ' 突然変異:', mut or 'なし')
    # 自己検査：基線の W1' = W1（種ごと・相対 1e-12）
    worst = 0.0
    for s in ss:
        for y in YEARS:
            a, b = per[arms[0][0]][s][0][y], per[arms[0][0]][s][1][y]
            worst = max(worst, abs(a - b) / abs(a))
    print('自己検査 基線の W1\'÷W1−1 の最大 %.3e %s' % (worst, '一致' if worst <= 1e-12 else '不一致'))
    for k, nm in ((0, 'W1（今の値段）'), (1, "W1'（基線の値段）")):
        for y in YEARS:
            vals = [res[n][k][y] for n, _ in arms]
            w = max(vals) / min(vals)
            print('%s %s 中央値 ' % (nm, y) + '  '.join('%s %.5f' % (n, res[n][k][y]) for n, _ in arms) +
                  '  幅 %.4f  閾値 1+2*(幅-1) %.4f' % (w, 1 + 2 * (w - 1)))
    # 記録：種ごとの対の比（N÷基線の同じ種）
    for k, nm in ((0, 'W1'), (1, "W1'")):
        for y in YEARS:
            for n, _ in arms[1:]:
                r = sorted(per[n][s][k][y] / per[arms[0][0]][s][k][y] for s in ss)
                print('記録 種ごとの対の比 %s %s %s÷基線 最小 %.4f 中央 %.4f 最大 %.4f' % (nm, y, n, r[0], st.median(r), r[-1]))
    # N1・N2 の W1'÷W1（値段の取り替えの効き・突然変異 own では 1）
    for n, _ in arms[1:]:
        print('値段の取り替え %s ' % n + '  '.join("%s W1'÷W1 %.6f" % (y, res[n][1][y] / res[n][0][y]) for y in YEARS))
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
