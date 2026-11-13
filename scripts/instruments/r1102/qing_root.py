# (1102) 問い 1：清の O の根（L の高さか人口の算入か）と、門 C5・C9 の 3 腕の幅。表を読むだけ。
# 使い方: python -X utf8 -I qing_root.py <nat_M.tsv> <M の表のフォルダ> <nat_N1.tsv> <N1 のフォルダ> <nat_N2.tsv> <N2 のフォルダ> > qing_root_out.txt
# 入力：nat_agg.py の出力（種×四半期×国の L・W・H・O・人口K）と nation_s の armyThreatShare（SecurityShare(army)＝陸軍の不安÷(陸軍の不安＋自国の養える軍)・Instruments.cs:2432）。
# 式（設計 §3-3・計器 :195〜197 と同じ）：q_t＝ΣL÷ΣpopK・wUse＝min(W,L)・H＝min(L−wUse, popK×q)・O＝L−wUse−H。
# 分け方（事前登録 d1102_work/prereg.txt 問い 1）：平時は ΣO＝Σ_余りの国 popK_n×q_t×(d_n−1)、d_n＝(L_n÷popK_n)÷q_t。
#   反実仮想 A〈L の高さを並にする〉＝清の (d−1) を他の余りの国の人口加重の (d−1) へ替えた時の清の取り分。
#   反実仮想 B〈人口を並にする〉＝清の popK を他の余りの国の平均の popK へ替えた時の清の取り分（清の d はそのまま）。
#   備えの形〈R1〉＝O′＝O×(1−a_n)（a＝armyThreatShare・新しい係数 0・AI の案）。
import sys, collections, statistics as st, os
import sys as _s; _s.stdout.reconfigure(newline='\n')  # 出力は LF
QING = '清'
QS = [0, 20, 40, 100, 200, 300, 400, 500, 600, 660]
def med(v): return st.median(v) if v else float('nan')
def load_threat(folder):
    th = {}
    for s in range(1, 11):
        p = os.path.join(folder, f'nation_s{s}.tsv')
        if not os.path.exists(p): continue
        with open(p, encoding='utf-8') as f:
            h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
            for line in f:
                c = line.rstrip('\n').split('\t')
                try: th[(s, int(c[ix['quarter']]), c[ix['nation']])] = float(c[ix['armyThreatShare']])
                except ValueError: pass
    return th
def analyse(natp, folder, label):
    th = load_threat(folder)
    data = collections.defaultdict(dict)
    with open(natp, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in f:
            c = line.rstrip('\n').split('\t')
            data[(int(c[0]), int(c[1]))][c[ix['nation']]] = {k: float(c[ix[k]]) for k in ('L', 'WA', 'H', 'O', 'popK')}
    rec = collections.defaultdict(lambda: collections.defaultdict(list)); miss_th = 0; n_th = 0
    big = []; c9q = collections.Counter(); c9all = 0; c9red = 0; c9red_r1 = 0; c9red_fix = 0; qing_top = 0
    for (s, q), nat in data.items():
        totL = sum(v['L'] for v in nat.values()); totP = sum(v['popK'] for v in nat.values())
        if not (totL > 0 and totP > 0): continue
        qt = totL / totP
        sumO = sumO1 = sumOf = 0.0; surplus = []
        for nm, v in nat.items():
            wu = min(v['WA'], v['L']); hh = min(max(0.0, v['L'] - wu), v['popK'] * qt); o = max(0.0, v['L'] - wu - hh)
            a = th.get((s, q, nm)); n_th += 1
            if a is None: miss_th += 1; a = 0.0
            v['Ot'] = o; v['O1'] = o * (1 - a); v['a'] = a; v['d'] = (v['L'] / v['popK']) / qt if v['popK'] > 0 else float('nan')
            sumO += o; sumO1 += v['O1']; sumOf += v['O']
            if o > 0 and nm != QING: surplus.append(v)
        c9all += 1
        if sumO > 0:
            mx = max(nat.items(), key=lambda kv: kv[1]['Ot'])
            if mx[1]['Ot'] / sumO >= 0.3: c9red += 1; c9q[mx[0]] += 1
            if mx[0] == QING and mx[1]['Ot'] / sumO >= 0.3: qing_top += 1
        if sumO1 > 0 and max(v['O1'] for v in nat.values()) / sumO1 >= 0.3: c9red_r1 += 1
        if sumOf > 0 and max(v['O'] for v in nat.values()) / sumOf >= 0.3: c9red_fix += 1
        qg = nat.get(QING)
        if qg is None: continue
        sh = qg['Ot'] / sumO if sumO > 0 else 0.0
        sh1 = qg['O1'] / sumO1 if sumO1 > 0 else 0.0
        shf = qg['O'] / sumOf if sumOf > 0 else 0.0
        exL = totL - qg['L']; exP = totP - qg['popK']
        dex = (qg['L'] / qg['popK']) / (exL / exP) if qg['popK'] > 0 and exP > 0 and exL > 0 else float('nan')
        rec[q]['qt'].append(qt); rec[q]['ΣO÷ΣL'].append(sumO / totL); rec[q]['清の取り分 q_t'].append(sh); rec[q]['清 q 固定'].append(shf); rec[q]['清 備え R1'].append(sh1)
        rec[q]['清の d'].append(qg['d']); rec[q]['清を除いた q での d'].append(dex); rec[q]['清の a'].append(qg['a'])
        rec[q]['清の人口の取り分'].append(qg['popK'] / totP); rec[q]['清の L の取り分'].append(qg['L'] / totL)
        if surplus:
            sp = sum(v['popK'] for v in surplus)
            dbar = sum(v['popK'] * (v['d'] - 1) for v in surplus) / sp
            abar = sum(v['popK'] * v['a'] for v in surplus) / sp
            rec[q]['他の余りの国の (d−1)'].append(dbar); rec[q]['他の余りの国の a'].append(abar)
            rec[q]['清の人口÷余りの国の人口'].append(qg['popK'] / (sp + (qg['popK'] if qg['Ot'] > 0 else 0)))
        if sh > 0.6 and surplus:
            sp = sum(v['popK'] for v in surplus); others = sum(v['Ot'] for v in surplus)
            dbar = sum(v['popK'] * (v['d'] - 1) for v in surplus) / sp
            oA = qg['popK'] * qt * dbar; shA = oA / (oA + others) if oA + others > 0 else 0
            oB = (sp / len(surplus)) * qt * (qg['d'] - 1); shB = oB / (oB + others) if oB + others > 0 else 0
            big.append((s, q, sh, qg['d'], dbar + 1, qg['popK'] / (sp + qg['popK']), shA, shB, len(surplus)))
    print(f'\n## {label}（{natp}）')
    print(f'nation_s の a が見つからない国×四半期 {miss_th}/{n_th}（名で結んだ・見つからない国は a＝0）')
    keys = ['qt', 'ΣO÷ΣL', '清の取り分 q_t', '清 q 固定', '清 備え R1', '清の d', '清を除いた q での d', '他の余りの国の (d−1)', '清の人口の取り分', '清の L の取り分', '清の人口÷余りの国の人口', '清の a', '他の余りの国の a']
    print('四半期\t' + '\t'.join(keys))
    for q in QS:
        if q not in rec: continue
        print(f'{q}\t' + '\t'.join(f'{med(rec[q][k]):.4g}' for k in keys))
    allq = sorted(rec)
    ser = [med(rec[q]['ΣO÷ΣL']) for q in allq]
    print(f'ΣO÷ΣL（q_t・種の中央）の全ての四半期：最小 {min(ser):.4g}・最大 {max(ser):.4g}（四半期 {allq[ser.index(max(ser))]}）')
    qsh = [med(rec[q]['清の取り分 q_t']) for q in allq]
    print(f'清の取り分（q_t・種の中央）の全ての四半期：最大 {max(qsh):.4g}（四半期 {allq[qsh.index(max(qsh))]}）・0.3 以上の四半期 {sum(1 for x in qsh if x >= 0.3)}/{len(qsh)}')
    print(f'C9〈国ごとの O の世界の取り分の最大 ＜0.3〉が破れる種×四半期：q_t {c9red}/{c9all}（うち清 {qing_top}）・q 固定 {c9red_fix}・備え R1 {c9red_r1}・破る国の上位 {c9q.most_common(6)}')
    if big:
        print(f'清の取り分＞0.6 の種×四半期 {len(big)}：清の d の中央 {med([b[3] for b in big]):.4g}・他の余りの国の人口加重の d {med([b[4] for b in big]):.4g}・清の人口÷余りの人口 {med([b[5] for b in big]):.4g}・余りの国の数（清を除く）{med([b[8] for b in big]):.4g}')
        print(f'  反実仮想 A（清の d を他の余りの国の並に）で清の取り分の中央 {med([b[6] for b in big]):.4g}・B（清の人口を余りの国の平均に）{med([b[7] for b in big]):.4g}')
a = sys.argv[1:]
for i, lab in zip(range(0, len(a), 2), ['M', 'N1', 'N2']):
    analyse(a[i], a[i + 1], lab)
