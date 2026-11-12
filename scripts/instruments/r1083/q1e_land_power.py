# (1083) 問い 1 の補い：世界の陸軍力の和 ΣL と国の L の四半期 0〜40 の動き（種 1〜10 の中央）・冬に首都からの距離が inf になる州の名。
# 使い方: python -X utf8 -I q1e_land_power.py <表のフォルダ> > q1e_out.txt
import sys, os, collections, statistics as st
import sys as _s; _s.stdout.reconfigure(newline='\n')  # 出力は LF
d = sys.argv[1]; QS = [0, 1, 2, 3, 4, 8, 12, 20, 40]
sl = collections.defaultdict(list); nl = collections.defaultdict(lambda: collections.defaultdict(list)); infr = collections.Counter()
for s in range(1, 11):
    fi = open(os.path.join(d, f'rulepower_s{s}.tsv'), encoding='utf-8'); h = fi.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
    seen = set(); tot = collections.defaultdict(float)
    for line in fi:
        c = line.rstrip('\n').split('\t'); q = int(c[0])
        if q > max(QS): break
        if q == 0 and c[ix['directKm']] == 'inf': infr[(c[ix['nation']], c[ix['region']])] += 1
        k = (q, c[ix['nation']])
        if q in QS and k not in seen:
            seen.add(k); L = float(c[ix['lNationIndex']]); tot[q] += L; nl[c[ix['nation']]][q].append(L)
    for q in QS: sl[q].append(tot[q])
print('四半期\t' + '\t'.join(str(q) for q in QS))
print('ΣL\t' + '\t'.join(f'{st.median(sl[q]):.3g}' for q in QS))
for n in ('フランス', '日本', 'オーストリア', 'アメリカ', 'イギリス', 'スペイン', 'ロシア', '清', 'マラータ同盟'):
    print(f'{n}\t' + '\t'.join(f'{st.median(nl[n][q]):.3g}' if nl[n][q] else '-' for q in QS))
print('四半期 0 に首都からの距離が inf の州（種の数）', sorted(infr.items()))
