"""(210) 読み手（読むだけ）＝1914 年（Q524）の導出政体の分布と、議会の実効統制・国債の世界全体のずれ（腕ごと）。
使い方：PYTHONUTF8=1 python an_pol210.py <d210 フォルダ> <種の起点> <種の数> [ラベル]"""
import sys, os, collections, statistics as st
PN = ['絶対', '専制', '幕府', '立憲', '共和', 'ブル独', 'プロ独', '軍政', '無政府']
FQ = 523
d, s0, ns = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]); lab = sys.argv[4] if len(sys.argv) > 4 else ''
# 表の exact の型（SimCore の表を読まずに N 行の ex と導出から推す＝ex=1 のときの導出の集合を国ごとに集める）
pol = collections.Counter(); polw = collections.Counter(); parl = []; debt = []; leg = []; subj = 0; tot = 0
exn = collections.Counter(); exs = collections.defaultdict(set)
yearly = collections.defaultdict(lambda: collections.Counter())
for s in range(s0, s0 + ns):
    best = {}
    with open(os.path.join(d, f'd{s}.txt'), encoding='utf-8') as f:
        for line in f:
            if line[0] != 'N': continue
            t = line.rstrip('\n').split('|')
            q = int(t[1])
            if q % 40 == 3 and int(t[7]) >= 0:
                yearly[q][PN[int(t[5])]] += 1
            if q != FQ: continue
            polw[PN[int(t[5])]] += 1
            if int(t[7]) < 0: continue
            if t[3] not in best or float(t[16]) > float(best[t[3]][16]): best[t[3]] = t
    for nat, t in best.items():
        tot += 1; pol[PN[int(t[5])]] += 1
        if t[6]: subj += 1
        parl.append(float(t[10])); leg.append(float(t[9]))
        dg = float(t[17]) if t[17] != 'nan' else float('nan')
        if dg == dg: debt.append(dg)
        if int(t[7]) == 1: exs[nat].add(PN[int(t[5])]); exn[nat] += 1
print(f'# {lab}（種 {s0}..{s0+ns-1}）1914 年（Q524）')
print(f'表の国×種 {tot}・導出政体 ' + '・'.join(f'{k} {v}' for k, v in pol.most_common()) + f'・宗主あり {subj}')
print(f'全ての国 ' + '・'.join(f'{k} {v}' for k, v in polw.most_common()))
print(f'議会の実効統制 平均 {st.mean(parl):.3f}・中央 {st.median(parl):.3f}・0.5 以上 {sum(1 for x in parl if x >= 0.5)}／正統性 平均 {st.mean(leg):.3f}／国債÷年GDP 中央 {st.median(debt):.3f}・0.083（月の GDP 1つ）以上 {sum(1 for x in debt if x >= 1/12)}/{len(debt)}')
print('10年ごとの導出政体（表の国・全ての同名の国）＝' )
for q in sorted(yearly):
    c = yearly[q]; n = sum(c.values())
    print(f'  {1783 + (q + 1) / 4:.0f}: ' + '・'.join(f'{k} {100*v/n:.0f}%' for k, v in c.most_common(4)))
