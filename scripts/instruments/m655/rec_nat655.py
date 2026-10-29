"""(655) 合流の記録（読むだけ）：主要 9 国の人口の倍率（1913÷1783・nation_s の populationK・年の最後の四半期）と 1913 年の政体の番号（polityCode の最頻）を腕ごとに並べる（線 inh の事前登録 ⑦ の国ごとの並べ・ヌル腕の幅と比べるため）。
使い方：python -X utf8 -I rec_nat655.py <腕名>=<計器の出力フォルダ> ...（種は 1〜10）。⚠表示名の完全一致で引く（改名・建国の国は数えない）。"""
import csv, os, sys, statistics as st, collections
sys.stdout.reconfigure(encoding='utf-8'); csv.field_size_limit(10**9)
N9 = ['清', 'ロシア', 'オスマン', 'フランス', 'イギリス', 'プロイセン', 'オーストリア', '日本', 'アメリカ']
def load(d):
    ratio = collections.defaultdict(list); pol = collections.defaultdict(list)
    for s in range(1, 11):
        p = os.path.join(d, f'nation_s{s}.tsv')
        if not os.path.exists(p): continue
        last = {}
        with open(p, encoding='utf-8', newline='') as f:
            r = csv.reader(f, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
            for x in r:
                if len(x) != len(h): continue
                n = x[ix['nation']]
                if n not in N9: continue
                y = int(x[ix['year']])
                if y not in (1783, 1913): continue
                q = int(x[ix['quarter']])
                if (n, y) not in last or q >= last[(n, y)][0]: last[(n, y)] = (q, float(x[ix['populationK']]), x[ix['polityCode']] if 'polityCode' in ix else '-')
        for n in N9:
            a = last.get((n, 1783)); b = last.get((n, 1913))
            if a and b and a[1] > 0: ratio[n].append(b[1] / a[1]); pol[n].append(b[2])
    return ratio, pol
arms = [a.split('=', 1) for a in sys.argv[1:]]
res = {n: load(d) for n, d in arms}
print('# 人口の倍率 1913÷1783（種の中央値〔生きた種の数〕）と 1913 年の政体の番号の最頻　腕の並び ' + ' / '.join(n for n, _ in arms))
for nat in N9:
    cells = []
    for n, _ in arms:
        rt, pl = res[n]
        v = rt[nat]
        cells.append((f'{st.median(v):.2f}〔{len(v)}〕' if v else '-') + ' 政体' + (collections.Counter(pl[nat]).most_common(1)[0][0] if pl[nat] else '-'))
    print(f'  {nat}\t' + '\t'.join(cells))
