# (900) c77lg：nation_s の openFrontStates（種類.軸:符号・(274)）を軸ごとに数える（読むだけ）。
# 符号：-1 未記録／0 閉じていた／1 純支持で否決／2 政治資本で否決／3 無償で動いた／4 払って動いた／5 不可逆で固定／6 前提で固定／7 無償で届いた／8 払って届いた／9 軍事費の失効
# 使い方: python -X utf8 s19_fronts.py <生のフォルダ> [種の数]
import sys, os, collections
d = sys.argv[1]; ns = int(sys.argv[2]) if len(sys.argv) > 2 else 10
cnt = collections.defaultdict(collections.Counter); pb = collections.Counter(); nrow = 0; nfront = collections.Counter()
for s in range(1, ns + 1):
    f = open(os.path.join(d, f'nation_s{s}.tsv'), 'rb'); h = f.readline().decode().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
    for raw in f:
        r = raw.decode().rstrip('\n').split('\t'); nrow += 1
        st = r[ix['openFrontStates']]
        try: n = int(r[ix['openFrontsCount']]); nfront[min(n, 12)] += 1
        except ValueError: pass
        try:
            b = float(r[ix['pushBudgetFactor']]); pb['<1' if b < 1 - 1e-12 else '=1'] += 1
        except ValueError: pass
        if not st: continue
        for it in st.split(';'):
            k, _, code = it.rpartition(':')
            cnt[k][code] += 1
print('行', nrow, '押す力の係数', dict(pb), '開いた戦線の数（12 以上は 12）', dict(sorted(nfront.items())))
codes = ['-1', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '9x', '0x']
print('戦線\t' + '\t'.join(codes))
for k in sorted(cnt):
    print(k + '\t' + '\t'.join(str(cnt[k].get(c, 0)) for c in codes))
