"""(210) 読み手（読むだけ）＝国の群の10年ごとの時系列（導出政体が議会側の割合・議会の実効統制・国債が月の GDP 1つ以上の割合・正統性・宗主あり）。
使い方：PYTHONUTF8=1 python an_ts210.py <d210 フォルダ> <種の起点> <種の数> <国,国,...|ALL> [ラベル]"""
import sys, os, collections, statistics as st
d, s0, ns = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]); grp = sys.argv[4]; lab = sys.argv[5] if len(sys.argv) > 5 else ''
G = None if grp == 'ALL' else set(grp.split(','))
acc = collections.defaultdict(lambda: [0, 0, 0.0, 0, 0.0, 0, 0])   # dec -> n, parlside, parlEff, debt>=1m, leg, subj, cw
for s in range(s0, s0 + ns):
    with open(os.path.join(d, f'd{s}.txt'), encoding='utf-8') as f:
        for line in f:
            if line[0] != 'N': continue
            t = line.rstrip('\n').split('|')
            q = int(t[1])
            if q % 4 != 3: continue
            if G is not None and t[3] not in G: continue
            if G is None and int(t[7]) < 0: continue
            dec = int((1783 + (q + 1) / 4) // 10 * 10)
            a = acc[dec]
            a[0] += 1
            if int(t[5]) in (3, 4): a[1] += 1
            a[2] += float(t[10])
            dg = float(t[17]) if t[17] != 'nan' else 0.0
            if dg >= 1 / 12: a[3] += 1
            a[4] += float(t[9])
            if t[6]: a[5] += 1
            if t[12] == '1': a[6] += 1
print(f'# {lab}（種 {s0}..{s0+ns-1}・{grp[:40]}）10年ごと＝議会側（立憲・共和）の割合・議会の実効統制の平均・国債が月の GDP 1つ以上の割合・正統性・宗主あり・内戦')
for dec in sorted(acc):
    a = acc[dec]; n = a[0]
    print(f'{dec}s: 議会側 {100*a[1]/n:.0f}%・実効 {a[2]/n:.3f}・国債≥月GDP {100*a[3]/n:.0f}%・正統性 {a[4]/n:.3f}・宗主あり {100*a[5]/n:.0f}%・内戦 {100*a[6]/n:.1f}%')
