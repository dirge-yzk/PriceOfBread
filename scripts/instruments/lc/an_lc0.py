# (365) 段0: 建設・保守の機関車の需要の門。入力 = 計器つき10種の生の出力（腕 A＝ce3 の A・m348_ins と全行一致）の ins フォルダ。読むだけ。
# 使い方: PYTHONUTF8=1 python an_lc0.py <insフォルダ> <出力txt>
import csv, sys, collections, statistics as st

ins = sys.argv[1]
SEEDS = range(1, 11)
HIST = {  # 国: (最初の鉄道の年, 確かさ)  S=検索で確認  M=AI の記憶（未確認）
    'イギリス': (1825, 'S'), 'フランス': (1827, 'S'), 'アメリカ': (1830, 'M'), 'プロイセン': (1838, 'M'), 'バイエルン': (1835, 'S'),
    'ネーデルラント': (1839, 'S'), 'ロシア': (1837, 'S'), 'オーストリア': (1837, 'S'), 'スウェーデン': (1856, 'S'), 'デンマーク': (1847, 'M'),
    'スペイン': (1848, 'S'), 'ポルトガル': (1856, 'M'), 'オスマン': (1856, 'M'), '日本': (1872, 'S'), '清': (1876, 'S'), 'シャム': (1893, 'S'),
    'スイス': (1847, 'M'), 'サルデーニャ＝ピエモンテ': (1848, 'M'), '両シチリア': (1839, 'M'), 'ザクセン': (1839, 'M'),
    'ハノーファー': (1843, 'M'), 'ヴュルテンベルク': (1845, 'M'), 'ペルシャ': (1888, 'M'), '朝鮮': (1899, 'M'),
}


def rd(name, s):
    with open(f"{ins}/{name}_s{s}.tsv", encoding='utf-8', newline='') as f:
        return list(csv.DictReader(f, delimiter='\t'))


def med(v):
    return st.median(v) if v else None


open_q = {}
steam_q = {}
rail_first = {}
tlmap = {}
lastq = collections.defaultdict(int)
for s in SEEDS:
    for r in rd('disc', s):
        if r['kind'] == 'E' and r['row'] == '機関車':
            open_q[(s, r['nation'])] = (int(r['quarter']), int(r['year']), float(r['techTerm']), float(r['needTerm']), float(r['transmitTerm']))
        if r['kind'] == 'E' and r['row'] == '蒸気機関':
            steam_q[(s, r['nation'])] = int(r['quarter'])
    for r in rd('stock', s):
        t = r['time']
        if t.startswith('q'):
            q = int(t[1:])
            tlmap[(s, r['nation'], q)] = float(r['techLevelIndex'])
            if q > lastq[(s, r['nation'])]:
                lastq[(s, r['nation'])] = q
            if float(r['railCapacityStock']) > 0 and (s, r['nation']) not in rail_first:
                rail_first[(s, r['nation'])] = (q, int(r['year']))

def derived(n):
    return n.startswith(('共和制', '立憲王政', '統領制'))
nations = sorted(n for n in ({n for (_, n) in open_q} | {n for (_, n) in rail_first}) if not derived(n) and (n in HIST or sum(1 for s in SEEDS if (s, n) in open_q) >= 1))
out = []
P = out.append
P('# (365) 段0: 建設・保守の機関車の需要の門（腕 A＝既定の世界・10 種・計器つき・読むだけ）')
P('')
P('## 1. 機関車の行（発見の表）が開いた国×種の数と、その時の 3 項（技術力 t・必要性 n・伝わり x）')
tot = len(open_q)
selfsuff = sum(1 for v in open_q.values() if v[2] + v[3] >= 100)
P(f'国×種の開いた件数 {tot}（61 国×10 種=610 のうち）・伝わり無しで 100 に届く（t+n>=100）件 {selfsuff}（{selfsuff/tot:.1%}）・伝わりが決め手（t+n<100）{tot-selfsuff}（{(tot-selfsuff)/tot:.1%}）')
dep = [v for v in open_q.values() if v[2] + v[3] < 100]
P(f'伝わりが決め手の件: x の中央 {med([v[4] for v in dep]):.1f}・不足 100-(t+n) の中央 {med([100-v[2]-v[3] for v in dep]):.1f}')
P(f'蒸気機関の行は {sum(1 for v in steam_q.values() if v == 0)}/{len(steam_q)} 件が Q0（開幕）に解禁済み')
P('')
P('## 2. 国別（10 種の中央値）: 機関車の行が開いた年・最初の鉄道容量の年（railCapacityStock>0）・史実の最初の鉄道の年')
P('国\t行の開いた種\t行の開いた年(中央)\t最初の鉄道容量の年(中央)\t史実\t確かさ\tA−史実(鉄道容量)\t伝わり決め手の種\tt+n(中央)')
for n in nations:
    oy = [open_q[(s, n)][1] for s in SEEDS if (s, n) in open_q]
    ry = [rail_first[(s, n)][1] for s in SEEDS if (s, n) in rail_first]
    d = sum(1 for s in SEEDS if (s, n) in open_q and open_q[(s, n)][2] + open_q[(s, n)][3] < 100)
    tn = [open_q[(s, n)][2] + open_q[(s, n)][3] for s in SEEDS if (s, n) in open_q]
    h = HIST.get(n)
    mry = med(ry)
    hs = f'{h[0]}\t{h[1]}\t{(mry - h[0]):+.0f}' if (h and mry) else '-\t-\t-'
    P(f'{n}\t{len(oy)}/10\t{med(oy) if oy else "-"}\t{mry if mry else "-"}({len(ry)}/10)\t{hs}\t{d}\t{("%.0f" % med(tn)) if tn else "-"}')
P('')
P('史実の最初の鉄道の年: S=ウェブで確認（英 1825・仏 1827・バイエルン 1835・蘭 1839・露 1837・墺〔北部鉄道〕1837・西 1848・瑞典 1856・日 1872・清 1876〔呉淞〕・シャム 1893〜94）・M=AI の記憶（未確認・要出典）。国名が変わる国（共和制〜・立憲王政〜・統領制〜）は表から外した。')


P('')
P('## 2b. 事前登録 H1 の数え方で読んだ A（開かなかった種は 1951 と数えた 10 種の中央値）＝腕の比べの基線はこちら（表 2 の中央は開いた種だけ）')
P('国	史実	開いた種	y_c(A・H1 の規則)	d_c(A)=|y_c−史実|')
dA = []
for n in nations:
    h = HIST.get(n)
    if not h or h[1] != 'S':
        continue
    ys = [open_q[(s, n)][1] if (s, n) in open_q else 1951 for s in SEEDS]
    y = med(ys)
    dA.append(abs(y - h[0]))
    P(f'{n}	{h[0]}	{sum(1 for s in SEEDS if (s, n) in open_q)}/10	{y}	{abs(y - h[0]):.1f}')
P(f'出典つき 11 国の d_c(A) の中央 {med(dA):.1f} 年')
P('')
P('## 3. 伝わりの接触（disccontact_s・行=機関車）: 買い手が行を未解禁の四半期の分')
pre = collections.Counter(); post = collections.Counter(); buyers = collections.defaultdict(set)
rowall = collections.Counter(); rowpre = collections.Counter()
for s in SEEDS:
    for r in rd('disccontact', s):
        b = r['buyer']; q = int(r['quarter']); v = float(r['valuePerQuarter']); row = r['row']
        rowall[row] += v
        if row == '機関車':
            oq = open_q.get((s, b))
            if oq is None or q < oq[0]:
                pre[b] += v; buyers[s].add(b)
            else:
                post[b] += v
sp, so = sum(pre.values()), sum(post.values())
P(f'行=機関車の接触の額（10 種の合計）: 買い手が未解禁の四半期 {sp:.0f}・解禁後 {so:.0f}（未解禁の分 {sp/(sp+so+1e-9):.1%}）')
P(f'接触を持った未解禁の買い手の国数（種ごとの中央）{med([len(v) for v in buyers.values()])}')
P('行ごとの接触の額の合計（10 種・全期間・上位14）: 運び手の財の内訳は A の計器に無い（carrier 列は枝 g4）')
for row, v in rowall.most_common(14):
    P(f'  {row}\t{v:.0f}')

P('')
P('## 4. 支出（spend_s・資本家）: 建設に付く線路と機関車(129)・インフラ保守(130) の額のうち、その国の機関車の行が開く前の期間の分')
pre129 = pre130 = tot129 = tot130 = 0.0
for s in SEEDS:
    for r in rd('spend', s):
        if r['kind'] != 'extra' or r['slotId'] not in ('129', '130'):
            continue
        n = r['nation']; lq = int(r['lastQuarter']); fq = int(r['firstQuarter'])
        v = float(r['payPerMonth']) * float(r['monthsCount'])
        oq = open_q.get((s, n)); oqq = oq[0] if oq else 10 ** 9
        f = 1.0 if lq < oqq else ((oqq - fq) / (lq - fq + 1) if fq < oqq else 0.0)
        if r['slotId'] == '129':
            tot129 += v; pre129 += v * f
        else:
            tot130 += v; pre130 += v * f
P(f'129 線路と機関車（建設）: 全期間 {tot129:.3g}・行が開く前 {pre129:.3g}（{pre129/tot129:.1%}）')
P(f'130 インフラ保守（セメント・線路・機関車0.5・要員・石炭）: 全期間 {tot130:.3g}・行が開く前 {pre130:.3g}（{pre130/tot130:.1%}）')
P('参考: 機関車の取り分は ConsLoco 0.12×機関車の価格 ÷（ConsRail 0.25×線路の価格＋ConsLoco 0.12×機関車の価格）。')

P('')
P('## 5. 反事実（読みの目安・AI の推定）: 伝わり x=0 のとき行は自力で開くか')
P('開く条件 t+n>=100。t は技術水準 TL に比例（t=70×min(1,TL/1.4)×push・push=1 と置く）。n は開いた時のまま。TL は 1950 年（最終四半期）の値で外挿の最大。')
P('国\tt+n(開いた時,中央)\tTL_最終(中央)\t外挿の最大 t+n(中央)\t1950 までに自力で開く種')
for n in nations:
    a = []; b = []; tls = []; c = 0
    for s in SEEDS:
        if (s, n) not in open_q:
            continue
        q0, y0, t, nn, x = open_q[(s, n)]
        tl0 = tlmap.get((s, n, q0)); tlf = tlmap.get((s, n, lastq[(s, n)]))
        if tl0 and tlf and tl0 > 0:
            tmax = min(70.0, t * tlf / tl0)
            a.append(t + nn); b.append(tmax + nn); tls.append(tlf)
            if tmax + nn >= 100:
                c += 1
    if a:
        P(f'{n}\t{med(a):.0f}\t{med(tls):.2f}\t{med(b):.0f}\t{c}/{len(a)}')

P('')
P('## 5b. 反事実の開く年（x=0・伝わりが全部消えたとして・上限の遅れ）: 開いた時の t/TL と n を固定し、TL(q) の伸びで t+n>=100 になる最初の四半期の年（push=1・t は 70 で頭打ち）')
sA = []; sB = []; sn = []
P('国	史実	A の行の年(中央)	x=0 の行の年(中央・1950 までに開かない種は 1951 と数える)	開かない種	A−史実	(x=0)−史実	確かさ')
qyear = {}
for s in SEEDS:
    for r in rd('stock', s):
        if r['time'].startswith('q'):
            qyear[(s, int(r['time'][1:]))] = int(r['year'])
for n in nations:
    h = HIST.get(n)
    if not h:
        continue
    ay = []; cy = []; never = 0
    for s in SEEDS:
        if (s, n) not in open_q:
            continue
        q0, y0, t, nn, x = open_q[(s, n)]
        tl0 = tlmap.get((s, n, q0))
        if not tl0:
            continue
        ay.append(y0)
        found = None
        for q in range(q0, lastq[(s, n)] + 1):
            tlq = tlmap.get((s, n, q))
            if tlq is None:
                continue
            if min(70.0, t * tlq / tl0) + nn >= 100:
                found = qyear[(s, q)]
                break
        if found is None:
            never += 1; cy.append(1951)
        else:
            cy.append(found)
    if ay:
        P(f'{n}	{h[0]}	{med(ay)}	{med(cy)}	{never}/{len(ay)}	{med(ay)-h[0]:+.0f}	{med(cy)-h[0]:+.0f}	{h[1]}')
        if h[1] == 'S':
            sA.append(abs(med(ay) - h[0])); sB.append(abs(med(cy) - h[0])); sn.append(n)


if sA:
    worse = [n for n, a, b in zip(sn, sA, sB) if b - a > 10]
    P(f'出典つき(S) {len(sA)} 国: |A−史実| の中央 {med(sA):.0f} 年・|(x=0)−史実| の中央 {med(sB):.0f} 年・(x=0) が A より 10 年を超えて悪い国 {len(worse)}: {",".join(worse)}')
    P('（x=0 は伝わりが全て消える上限の遅れ。実際は蒸気機関の財の購入と技師の移動が残るので、これより早く開く【推】。）')

open(sys.argv[2], 'w', encoding='utf-8').write('\n'.join(out) + '\n')
print('\n'.join(out))
