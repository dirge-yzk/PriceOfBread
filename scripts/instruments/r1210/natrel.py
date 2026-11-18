# (1210) ① 比べの候補の素の数（年末の行＝全ての国が書く行だけ）。用法: natrel.py <生> <種の始め> <種の終わり> <出力>
# 候補 R＝その国の取り分÷世界の人口の重みの平均（同じ年末）・候補 C＝同じ文化圏の国の群（ドイツ語圏・イタリア語圏の名簿＝この台本の中の表）の人口の重みの平均
# 名簿は読みのための仮の群（エンジンの文化の札は読まない＝近似と名乗る）。
import sys, csv, statistics as st
raw, s0, s1, out = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), sys.argv[4]
GER = {'プロイセン', 'ザクセン', 'ハノーファー', 'バイエルン', 'ヴュルテンベルク', 'ライン諸邦', 'オーストリア', 'バーデン', 'ヘッセン', 'メクレンブルク'}
ITA = {'サルデーニャ＝ピエモンテ', '両シチリア', '教皇領', 'ヴェネツィア', 'トスカーナ', 'パルマ', 'モデナ', 'ジェノヴァ'}
YEARS = range(1783, 1951)
res = {}   # (who, year) -> list of values per seed
seen_g, seen_i = set(), set()
for s in range(s0, s1 + 1):
    by = {}
    with open(f'{raw}/ideology_s{s}.tsv', encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
        for x in r:
            y = int(x[ix['year']])
            by.setdefault(y, {})[x[ix['nation']]] = (float(x[ix['popK']]), float(x[ix['ideoNationalNationalismShare']]))   # 年の最後の行が残る
    for y, d in by.items():
        tw = sum(p for p, v in d.values()); wm = sum(p * v for p, v in d.values()) / tw if tw > 0 else None
        for grp, nm, key in ((GER, 'プロイセン', 'GER'), (ITA, 'サルデーニャ＝ピエモンテ', 'ITA')):
            g = {k: v for k, v in d.items() if k in grp}
            (seen_g if key == 'GER' else seen_i).update(g.keys())
            gw = sum(p for p, v in g.values()); gm = sum(p * v for p, v in g.values()) / gw if gw > 0 else None
            if nm in d and wm:
                res.setdefault((nm, y, 'share'), []).append(d[nm][1])
                res.setdefault((nm, y, 'R'), []).append(d[nm][1] / wm)
                if gm: res.setdefault((nm, y, 'C'), []).append(gm)
                res.setdefault((nm, y, 'world'), []).append(wm)
with open(out, 'w', encoding='utf-8', newline='\n') as o:
    o.write(f'# raw={raw} seeds={s0}..{s1}・年末の行・種の中央 [最大]・群の名乗り GER={sorted(seen_g)} ITA={sorted(seen_i)}\n')
    o.write('nation\tyear\tn\tshareMed\tshareMax\tworldMeanMed\tR_med\tR_max\tC_med\tC_max\n')
    for nm in ('プロイセン', 'サルデーニャ＝ピエモンテ'):
        for y in (1783, 1800, 1815, 1830, 1840, 1848, 1855, 1860, 1866, 1871, 1880, 1900, 1913, 1950):
            a = res.get((nm, y, 'share'))
            if not a: continue
            R = res[(nm, y, 'R')]; C = res.get((nm, y, 'C'), []); W = res[(nm, y, 'world')]
            o.write(f'{nm}\t{y}\t{len(a)}\t{st.median(a):.3f}\t{max(a):.3f}\t{st.median(W):.3f}\t{st.median(R):.2f}\t{max(R):.2f}\t' + (f'{st.median(C):.3f}\t{max(C):.3f}' if C else '\t') + '\n')
    # 1848〜1871 に R≥1 の年の割合・C≥0.25 の年の数（種ごと）
    for nm in ('プロイセン', 'サルデーニャ＝ピエモンテ'):
        tot = hitR = hitC = hitS = 0
        for y in range(1848, 1872):
            for k, lst in (('R', None),):
                pass
            R = res.get((nm, y, 'R'), []); C = res.get((nm, y, 'C'), []); S = res.get((nm, y, 'share'), [])
            tot += len(R); hitR += sum(1 for v in R if v >= 1.0); hitC += sum(1 for v in C if v >= 0.25); hitS += sum(1 for v in S if v >= 0.25)
        o.write(f'# {nm} 1848〜1871 の年末×種 {tot}：取り分≥0.25 {hitS}・R≥1 {hitR}・C≥0.25 {hitC}\n')
print('ok', out)
