# (1091) 旗 0 の盤面で 3 つの門が赤になることの紙の台本（rulepower_s の今の列だけ・G10 の値）。使い方: python -X utf8 -I gates0.py <表のフォルダ>
# 門〈同点〉＝持ち主の側と無統治の割合が相対 1e-9 で等しいのに bestClass が無統治の行／門〈凍結〉＝首都からの km が inf の行／門〈q〉＝q を開幕で固定した時の四半期 600 の ΣO÷ΣL（q_t なら 0.13〜0.23）
# 新しい列が入った後は gates.py（同じ場所）が R の列と凍結を外した列で同じ門を読む。
import sys, os, collections
d = sys.argv[1]
ties = inf = 0; rows = 0; sumO = collections.defaultdict(float); sumL = collections.defaultdict(float)
for s in range(1, 11):
    p = os.path.join(d, f'rulepower_s{s}.tsv')
    if not os.path.exists(p): continue
    fi = open(p, encoding='utf-8'); h = fi.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
    seen = set()
    for line in fi:
        c = line.rstrip('\n').split('\t'); rows += 1
        o = float(c[ix['ownerSideShare']]); u = float(c[ix['ungovSideShare']])
        if o == o and u == u and abs(o - u) <= 1e-9 * max(abs(o), abs(u), 1e-300) and c[ix['bestClass']] == 'ungov': ties += 1
        if c[ix['directKm']] == 'inf': inf += 1
        q = int(c[ix['quarter']]); n = c[ix['nation']]
        if q == 600 and (s, n) not in seen:
            seen.add((s, n)); sumO[s] += float(c[ix['oNationIndex']]); sumL[s] += float(c[ix['lNationIndex']])
r = sorted(sumO[s] / sumL[s] for s in sumO if sumL[s] > 0)
print('rows', rows, 'ties_ungov', ties, 'inf_km_rows', inf, 'q600_SigO_over_SigL_median_fixedq', r[len(r) // 2] if r else 'nan', 'seeds', len(r))
