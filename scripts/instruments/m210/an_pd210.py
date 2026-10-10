"""(210) 読み手（読むだけ）＝不履行の頻度を同じ単位で比べる（(207) の作者への報告②の (c)）。計器 inj_d210 の Q 行（国×四半期の Σ(1−r)）と D 行（全否認）。
シムの「部分の不履行」＝利払いを含む国の全ての請求を r の割合でしか払えない月（Fiscal は利子も r で払い、払えない利子は元本へ繰り入れる＝Q3）＝R&R の「期日に払えない」に当たる。
  滞りの窓＝国債がある（国債÷年GDP≧1%）四半期のうち r＜1 の月がある四半期の連なり（間の空きは R&R と同じく2年＝8四半期まで同じ窓）。
  窓の深さ＝窓の中の連続4四半期（1年）の Σ(1−r) の最大（月の請求の何か月分を1年で払えなかったか）。深さが線 θ 以上の窓を「部分の不履行」に数える（全否認で終わった窓も含む）。
  ⚠窓の大きさ（窓全体の Σ）で数えると、何十年も続く浅い残り（r 0.99x）が積もって数に入る＝R&R の「期日に払えない」出来事ではない＝深さで数える。
  2つの数え方＝all（門が開いていても r＜1 の月を全て）・gate（借入の門が閉じた月だけ＝不履行の計数と同じ月）。
史実＝Reinhart & Rogoff (2008) NBER WP 13882 の表2〜5（(205) が数えた主要10か国 1783〜1950 の31件）。全否認の史実＝露 1918（ソビエトの国債無効の布告）。
使い方：PYTHONUTF8=1 python an_pd210.py <d210 フォルダ> <種の起点> <種の数> [ラベル]
"""
import sys, os, collections, statistics as st

MAJ = ['イギリス', 'フランス', 'ロシア', 'プロイセン', 'オーストリア', 'オスマン', 'スペイン', '清', 'アメリカ', '日本']
RR = {'イギリス': [], 'アメリカ': [], 'フランス': [1788, 1812], 'ロシア': [1839, 1885, 1918], 'オスマン': [1876, 1915, 1931, 1940],
      'プロイセン': [1807, 1813, 1932, 1939], 'オーストリア': [1802, 1805, 1811, 1816, 1868, 1938, 1940], '日本': [1942],
      'スペイン': [1809, 1820, 1831, 1834, 1851, 1867, 1872, 1882], '清': [1921, 1939]}
RR_FULL = {'ロシア': [1918]}
TH = [0.05, 0.1, 0.25, 0.5, 1.0]
GAP = 8


def yr(q):
    return 1783 + (q + 1) / 4.0


def close(s, key, cur, D):
    # 窓の深さ＝連続4四半期の Σ(1−r) の最大
    qv = cur[4]; deep = 0.0
    for q, _ in qv:
        deep = max(deep, sum(v for qq, v in qv if q - 3 <= qq <= q))
    return (s, key[1], cur[0], cur[1], deep, any(cur[0] <= x <= cur[1] + 1 for x in D.get(key, [])), cur[3], cur[2])


def main():
    d, s0, ns = sys.argv[1], int(sys.argv[2]), int(sys.argv[3])
    lab = sys.argv[4] if len(sys.argv) > 4 else ''
    seeds = list(range(s0, s0 + ns))
    # 窓：mode -> list of (seed, base, q0, q1, size, ended_in_full, maxdepth)
    wins = {'all': [], 'gate': []}
    full = []
    for s in seeds:
        Q = collections.defaultdict(dict)   # (idx, base) -> q -> (sum1mr, cntSum, minR, debt)
        D = collections.defaultdict(list)
        with open(os.path.join(d, f'd{s}.txt'), encoding='utf-8') as f:
            for line in f:
                if line[0] == 'Q':
                    t = line.rstrip('\n').split('|')
                    q = int(t[1]); key = (t[2], t[3])
                    debt = float(t[11]) if t[11] != 'nan' else 0.0
                    Q[key][q] = (float(t[6]), float(t[8]), float(t[9]), debt)
                elif line[0] == 'D':
                    t = line.rstrip('\n').split('|')
                    D[(t[2], t[4])].append(int(t[1]))
                    full.append((s, t[4], int(t[1]), int(t[3])))
        for key, qs in Q.items():
            for mode in ('all', 'gate'):
                cur = None
                for q in sorted(qs):
                    a, g, mn, debt = qs[q]
                    v = a if mode == 'all' else g
                    if not (v > 0) or (mode == 'all' and not (debt >= 0.01)):
                        continue
                    if cur is not None and q - cur[1] <= GAP:
                        cur[1] = q; cur[2] += v; cur[3] = min(cur[3], mn); cur[4].append((q, v))
                    else:
                        if cur is not None: wins[mode].append(close(s, key, cur, D))
                        cur = [q, q, v, mn, [(q, v)]]
                if cur is not None: wins[mode].append(close(s, key, cur, D))
    nrr = sum(len(v) for v in RR.values())
    print(f'# 不履行の頻度を同じ単位で（{lab}・種 {s0}..{s0+ns-1}・主要10か国・1783〜1950）')
    print(f'史実（R&R 2008 表2〜5）＝{nrr}件（部分の不履行と組み替えを含む）・うち全否認＝露 1918 の1件（ソビエトの国債無効の布告）')
    print()
    # 全否認（今の不履行）
    fm = [x for x in full if x[1] in MAJ]
    per = collections.Counter(x[0] for x in fm)
    vals = [per.get(s, 0) for s in seeds]
    print(f'## 全否認（今の不履行＝国債を全て消す）')
    print(f'主要10か国 {len(fm)}件（種あたり 平均 {st.mean(vals):.1f}・中央 {st.median(vals):.1f}）・史実の全否認 1件に対して比 {st.median(vals)/1:.1f}・R&R の{nrr}件に対して比 {st.median(vals)/nrr:.3f}')
    byn = collections.Counter(x[1] for x in fm)
    print('  国ごと（種あたり｜R&R）＝' + '・'.join(f'{n} {byn.get(n,0)/ns:.1f}｜{len(RR[n])}' for n in MAJ))
    print()
    for mode in ('all', 'gate'):
        W = [w for w in wins[mode] if w[1] in MAJ]
        print(f'## 部分の不履行（滞りの窓・{"r＜1 の全ての月〔国債÷年GDP≧1%〕" if mode == "all" else "借入の門が閉じた月だけ"}）')
        print('| 線 θ（1年の深さ＝月の請求の何か月分） | 窓（種の和） | 種あたり 平均・中央 | R&R 31件に対する比（中央） | うち全否認で終わった窓 | 国ごと（種あたり｜R&R） |')
        print('|---|---|---|---|---|---|')
        for th in TH:
            sel = [w for w in W if w[4] >= th]
            per = collections.Counter(w[0] for w in sel)
            vals = [per.get(s, 0) for s in seeds]
            byn = collections.Counter(w[1] for w in sel)
            print(f'| {th} | {len(sel)} | {st.mean(vals):.1f}・{st.median(vals):.1f} | {st.median(vals)/nrr:.3f} | {sum(1 for w in sel if w[5])} | ' + '・'.join(f'{n} {byn.get(n,0)/ns:.1f}｜{len(RR[n])}' for n in MAJ) + ' |')
        print()
        # 時代ごと（θ=0.25）
        sel = [w for w in W if w[4] >= 0.25]
        eras = [(1783, 1815), (1816, 1849), (1850, 1913), (1914, 1950)]
        print(f'  時代ごと（θ＝0.25・種の和÷種｜R&R）＝' + '・'.join(
            f'{a}-{b} {sum(1 for w in sel if a <= yr(w[2]) < b + 1)/ns:.1f}｜{sum(1 for n in MAJ for y in RR[n] if a <= y <= b)}' for a, b in eras))
        # 窓の大きさの分布
        sz = sorted(w[4] for w in W)
        if sz:
            print(f'  窓の深さの分位（主要10か国・全ての窓 {len(sz)}）＝10% {sz[len(sz)//10]:.3g}・50% {sz[len(sz)//2]:.3g}・90% {sz[9*len(sz)//10]:.3g}・最大 {sz[-1]:.3g}')
        print()
    # 主要10か国の窓の例（θ=0.25・all・種の起点）
    print(f'## 例（seed{s0}・all・θ≧0.25・主要10か国）＝国・始まりの年〜終わりの年・深さ（1年）・窓全体の Σ・r の最小・全否認で終わったか')
    for w in sorted([w for w in wins['all'] if w[0] == s0 and w[1] in MAJ and w[4] >= 0.25], key=lambda w: (w[1], w[2])):
        print(f'  {w[1]} {yr(w[2]):.0f}〜{yr(w[3]):.0f}・{w[4]:.2f}・{w[7]:.2f}・{w[6]:.2f}・{"全否認" if w[5] else "—"}')


main()
