# (1129) 合流：c109t0 の新しい列の幅を記録する（反証役 v の中〈新しい列の生を消した＝幅を読めない〉への答え・門ではない）。
#   python -X utf8 -I newcols1129.py <keep_1129/base_ins> > out
#   prodcap_s の profitFactor・lastInputFill・scale（空と値の数・最小・最大・[0,1] の外の件数）・s7staple_s の importOverDemand・shortageOverDemand（分位）・s4cost_s の fxBiasPinnedShare（分位）。
#   v の低〈眠り・倒産の空の判定は四半期の末の状態〉の件数（同じ月の後ろで状態が変わった行）は数えられる列が無い＝数えない。
import os, sys, math

ins = sys.argv[1]


def cols(path):
    with open(path, encoding='utf-8') as r:
        head = r.readline().rstrip('\n').split('\t')
        for line in r:
            yield head, line.rstrip('\n').split('\t')


def q(v, p):
    if not v:
        return float('nan')
    v = sorted(v)
    return v[min(len(v) - 1, int(p * (len(v) - 1) + 0.5))]


for table, names in (('prodcap_s', ('profitFactor', 'lastInputFill', 'scale', 'laborPopIndex')),
                     ('s7staple_s', ('importOverDemand', 'shortageOverDemand')),
                     ('s4cost_s', ('fxBiasPinnedShare', 'fxBiasObs', 'laborPriceGrowth10y'))):
    st = {n: {'blank': 0, 'nonfinite': 0, 'out01': 0, 'vals': []} for n in names}
    for s in range(1, 11):
        p = os.path.join(ins, f'{table}{s}.tsv')
        if not os.path.exists(p):
            continue
        idx = None
        for head, row in cols(p):
            if idx is None:
                idx = {n: head.index(n) for n in names}
            for n in names:
                x = row[idx[n]] if idx[n] < len(row) else ''
                if x == '':
                    st[n]['blank'] += 1
                    continue
                v = float(x)
                if not math.isfinite(v):
                    st[n]['nonfinite'] += 1
                    continue
                if v < 0 or v > 1:
                    st[n]['out01'] += 1
                # 分位は 1/50 の間引きで足りる（記録）
                if len(st[n]['vals']) < 4_000_000:
                    st[n]['vals'].append(v)
    for n in names:
        d = st[n]
        v = d['vals']
        print(f"{table}\t{n}\tblank {d['blank']}\tnonfinite {d['nonfinite']}\tvalues {len(v)}\tmin {min(v) if v else 'nan':.6g}\tp50 {q(v, .5):.6g}\tp99 {q(v, .99):.6g}\tmax {max(v) if v else 'nan':.6g}\toutside[0,1] {d['out01']}")
