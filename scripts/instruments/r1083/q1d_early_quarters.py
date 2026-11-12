# (1083) 問い 1 の補い：開幕の後の四半期 0〜12 で、影の外国の行の数・O の国・冬の凍結（directKm＝inf）の行の数がどう動くか（10 種）。
# 使い方: python -X utf8 -I q1d_early_quarters.py <表のフォルダ> > q1d_out.txt
import sys, os, collections
import sys as _s; _s.stdout.reconfigure(newline='\n')  # 出力は LF
d = sys.argv[1]; QMAX = 12
fore = collections.defaultdict(list); infk = collections.defaultdict(list); onat = collections.defaultdict(lambda: collections.defaultdict(list)); tot = collections.defaultdict(list)
for s in range(1, 11):
    fi = open(os.path.join(d, f'rulepower_s{s}.tsv'), encoding='utf-8'); h = fi.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
    cf = collections.Counter(); ci = collections.Counter(); seen = set(); cn = collections.Counter()
    for line in fi:
        c = line.rstrip('\n').split('\t'); q = int(c[0])
        if q > QMAX: break
        cn[q] += 1
        if c[ix['bestClass']] == 'foreign': cf[q] += 1
        if c[ix['directKm']] == 'inf': ci[q] += 1
        k = (q, c[ix['nation']])
        if k not in seen:
            seen.add(k); onat[c[ix['nation']]][q].append(float(c[ix['oNationIndex']]))
    for q in range(QMAX + 1): fore[q].append(cf[q]); infk[q].append(ci[q]); tot[q].append(cn[q])
print('四半期\t行(種1)\t影で外国が最大の行（10 種の延べ）\t首都からの距離が inf の行（延べ）')
for q in range(QMAX + 1): print(f'{q}\t{tot[q][0]}\t{sum(fore[q])}\t{sum(infk[q])}')
import statistics as st
print('\nO の中央（10 種）四半期 0〜8：国')
for n in ('フランス', '日本', 'オーストリア', 'アメリカ', 'プロイセン', 'サルデーニャ＝ピエモンテ', 'ロシア', '清', 'マラータ同盟'):
    print(f'  {n}\t' + '\t'.join(f'{st.median(onat[n][q]):.3g}' if onat[n][q] else '-' for q in range(9)))
