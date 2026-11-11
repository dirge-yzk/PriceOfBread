# (978) c88cs 門と紙の台本（読むだけ）：土地の持ち分 B-2b（露・普の国家の枠・旗 LandShareCrownSlot）
# 使い方：
#   python -X utf8 -I gates978.py paper <出力.txt>                 ＝ keep_975 の base_ins・n1・n2 で no-go の紙（旗 0 と候補の両方）
#   python -X utf8 -I gates978.py gates <腕の計器の生> <出力.txt> [<基準の生>]  ＝ 門 G-S1〜G-S4 と記録（基準を渡せば同じ盤面の新÷旧）
# 規則は事前登録 measurements/m978_20261109_prereg_cs.txt の [4]・[5] の写し（結果を見た後に変えない）。
# 限界（自己申告）：①露・普の 2 国だけ・種 1〜10・q0〜q11 ②crown の列が無い生（旧の木）では門は〈列が無い＝赤〉③歳入は nation_s の revenueBaseMeanPerMonth（(918)・(939) と同じ列＝基数）
import csv, io, statistics as st, sys

NAT = ['ロシア', 'プロイセン']
SEEDS = range(1, 11)
QS = [str(q) for q in range(12)]
# 候補の 5 枠（0 貴族・1 教会・2 農民・3 市民・4 国家）＝B-2-0 材料 :110・:113（AI の記憶・要出典確認）
FIVE = {'ロシア': [0.530, 0.0, 0.0, 0.0, 0.470], 'プロイセン': [0.609, 0.020, 0.076, 0.0, 0.294]}
KEEP = {'m975': 'D:/PoB_work/keep_975/base_ins', 'n1': 'D:/PoB_work/keep_975/n1', 'n2': 'D:/PoB_work/keep_975/n2'}
G1_LO, G1_HI = 0.99, 1.01
G2_MAX = 1e-9
G3_MAX = 1e-9
G4_MAX = 1.01


def rd(path):
    txt = open(path, 'rb').read().decode('utf-8-sig')
    rows = [r for r in csv.reader(io.StringIO(txt), delimiter='\t') if r and not r[0].startswith('#')]
    h = rows[0]
    return [dict(zip(h, r)) for r in rows[1:]], h


def fl(x):
    try:
        return float(x)
    except (TypeError, ValueError):
        return float('nan')


def cof(n, cand):
    v = FIVE[n]
    return v[4] / sum(v) if cand else 0.0


def paper(out):
    L = []
    say = L.append
    say('# (978) no-go の紙：露・普の q0〜q11 の crownExp÷歳入の基数（crownExp＝c×非賦存×持ち主が貴族×国内の正の地代・月あたり）の種 1〜10 の中央')
    say('# 規則（事前登録 [5]）：m975 の値がヌル腕の幅（m975・n1・n2 の中央の最大−最小・候補の c で）以下なら no-go（腕を回さない）')
    vals = {}
    for arm, base in KEEP.items():
        for n in NAT:
            for cand in (False, True):
                per = []
                for s in SEEDS:
                    ls, _ = rd(f'{base}/landshare_s{s}.tsv')
                    ns, _ = rd(f'{base}/nation_s{s}.tsv')
                    rev = {r['quarter']: fl(r['revenueBaseMeanPerMonth']) for r in ns if r['nation'] == n and r['quarter'] in QS}
                    xs = []
                    for r in ls:
                        if r['nation'] != n or r['quarter'] not in QS:
                            continue
                        m = fl(r['monthsInQuarter']) or 3.0
                        neN = fl(r['rentNonEndowByOwnerClassNoblePerQuarter']) / m
                        rv = rev.get(r['quarter'], float('nan'))
                        if rv > 0:
                            xs.append(cof(n, cand) * neN / rv)
                    per.append(st.fmean(xs) if len(xs) == 12 else float('nan'))
                vals[(arm, n, cand)] = st.median([x for x in per if x == x]) if any(x == x for x in per) else float('nan')
    for n in NAT:
        meds = [vals[(a, n, True)] for a in KEEP]
        width = max(meds) - min(meds)
        for cand in (False, True):
            v = vals[('m975', n, cand)]
            go = v > width
            say(f'{n}\t{"候補 c=%.4f" % cof(n, True) if cand else "旗 0（c=0）"}\tm975 {v:.6f}\tn1 {vals[("n1", n, cand)]:.6f}\tn2 {vals[("n2", n, cand)]:.6f}\t幅（候補）{width:.6f}\t{"go" if go else "no-go"}')
    open(out, 'wb').write(('\n'.join(L) + '\n').encode('utf-8'))
    print('\n'.join(L))


def load_arm(base):
    d = {}
    for s in SEEDS:
        try:
            ls, h = rd(f'{base}/landshare_s{s}.tsv')
        except FileNotFoundError:
            continue
        for r in ls:
            if r['nation'] in NAT:
                d[(s, r['nation'], r['quarter'])] = r
    return d


def gates(arm, out, base=None):
    L = []
    say = L.append
    A = load_arm(arm)
    need = ['crownExpectedPerQuarter', 'crownActualPerQuarter', 'crownMaxRelDiff', 'crownExpExpectedPerQuarter', 'crownExpMaxRelDiff', 'crownAllActualPerQuarter']
    any_row = next(iter(A.values()), None)
    if any_row is None or any(c not in any_row for c in need):
        say('列が無い（crown の列の無い生）＝G-S1〜G-S4 は全て赤')
        say('判定: 赤')
        open(out, 'wb').write(('\n'.join(L) + '\n').encode('utf-8')); print('\n'.join(L)); return 1
    ok = {}
    # G-S1 働いた：種×国×四半期（q0〜q11・期待＞0）で 実際÷期待 ∈ [0.99,1.01]・各 種×国 に期待＞0 の四半期が 1 つ以上（空なら赤）
    g1 = []; empty1 = []
    for s in SEEDS:
        for n in NAT:
            k = 0
            for q in QS:
                r = A.get((s, n, q))
                if r is None:
                    continue
                e, a = fl(r['crownExpectedPerQuarter']), fl(r['crownActualPerQuarter'])
                if e > 0:
                    k += 1; g1.append((a / e, s, n, q))
            if k == 0:
                empty1.append((s, n))
    lo1 = min(g1)[0] if g1 else float('nan'); hi1 = max(g1)[0] if g1 else float('nan')
    ok['G-S1'] = bool(g1) and not empty1 and G1_LO <= lo1 and hi1 <= G1_HI
    say(f'G-S1 働いた：実際÷期待 最小 {lo1:.9f} 最大 {hi1:.9f}（行 {len(g1)}・期待 0 の 種×国 {len(empty1)}）→ {"緑" if ok["G-S1"] else "赤"}')
    # G-S2 写し：期待＞0 の行の crownMaxRelDiff ≤ 1e-9・輸出は crownExpExpectedPerQuarter＞0 の行の crownExpMaxRelDiff ≤ 1e-9（輸出の行は全体で 1 つ以上）
    d2 = [fl(A[(s, n, q)]['crownMaxRelDiff']) for s in SEEDS for n in NAT for q in QS if (s, n, q) in A and fl(A[(s, n, q)]['crownExpectedPerQuarter']) > 0]
    d2x = [fl(A[(s, n, q)]['crownExpMaxRelDiff']) for s in SEEDS for n in NAT for q in QS if (s, n, q) in A and fl(A[(s, n, q)]['crownExpExpectedPerQuarter']) > 0]
    m2 = max(d2) if d2 else float('nan'); m2x = max(d2x) if d2x else float('nan')
    ok['G-S2'] = bool(d2) and not empty1 and bool(d2x) and m2 <= G2_MAX and m2x <= G2_MAX
    say(f'G-S2 写し：国内 最大 {m2:.3e}（行 {len(d2)}）・輸出 最大 {m2x:.3e}（行 {len(d2x)}）→ {"緑" if ok["G-S2"] else "赤"}')
    # G-S3 私の枠：全ての四半期の maxRelDiffSlotVsActual ≤ 1e-9・q0 の rgoFieldCount＞0（全ての 種×国）
    d3 = [(fl(r['maxRelDiffSlotVsActual']), s, n, q) for (s, n, q), r in A.items()]
    f0 = [fl(A[(s, n, '0')]['rgoFieldCount']) for s in SEEDS for n in NAT if (s, n, '0') in A]
    m3 = max(d3)[0] if d3 else float('nan')
    ok['G-S3'] = bool(d3) and len(f0) == 2 * len(SEEDS) and min(f0) > 0 and m3 <= G3_MAX
    say(f'G-S3 私の枠：maxRelDiffSlotVsActual 最大 {m3:.3e}（{max(d3)[1:] if d3 else "-"}）・q0 の rgoFieldCount 最小 {min(f0) if f0 else "nan"} → {"緑" if ok["G-S3"] else "赤"}')
    # G-S4 上限：期待＞0 の行で crownAllActual÷crownExpected ≤ 1.01（q0〜q11）
    g4 = [(fl(A[(s, n, q)]['crownAllActualPerQuarter']) / fl(A[(s, n, q)]['crownExpectedPerQuarter']), s, n, q)
          for s in SEEDS for n in NAT for q in QS if (s, n, q) in A and fl(A[(s, n, q)]['crownExpectedPerQuarter']) > 0]
    m4 = max(g4)[0] if g4 else float('nan')
    ok['G-S4'] = bool(g4) and not empty1 and m4 <= G4_MAX
    say(f'G-S4 上限：全ての RGO の国家の取り分÷期待 最大 {m4:.9f}（行 {len(g4)}）→ {"緑" if ok["G-S4"] else "赤"}')
    # 記録
    say('# 記録（門ではない）')
    for n in NAT:
        rows = [A[(s, n, q)] for s in SEEDS for q in QS if (s, n, q) in A]
        if not rows:
            continue
        say(f'{n}\tq0〜q11 の和（種 1〜10）：crownExpected {sum(fl(r["crownExpectedPerQuarter"]) for r in rows):.1f}・crownExpExpected {sum(fl(r["crownExpExpectedPerQuarter"]) for r in rows):.1f}'
            f'・crownDeficitState {sum(fl(r.get("crownDeficitStatePerQuarter")) for r in rows):.1f}・crownProppedRgoMonths {sum(fl(r.get("crownProppedRgoMonths")) for r in rows):.0f}・crownNationalizedSkipCount {sum(fl(r.get("crownNationalizedSkipCount")) for r in rows):.0f}')
        allr = [r for (s, nn, q), r in A.items() if nn == n]
        say(f'{n}\t全期間の和：crownProppedRgoMonths {sum(fl(r.get("crownProppedRgoMonths")) for r in allr):.0f}・crownDeficitState {sum(fl(r.get("crownDeficitStatePerQuarter")) for r in allr):.1f}'
            f'・鉱山 非賦存 {sum(fl(r.get("rentNonEndowMineralPerQuarter")) for r in allr):.1f}・賦存 {sum(fl(r.get("rentEndowMineralPerQuarter")) for r in allr):.1f}・輸出 {sum(fl(r.get("expRentMineralPerQuarter")) for r in allr):.1f}')
    if base:
        B = load_arm(base)
        say('# 同じ盤面の新÷旧（q0〜q11 の和・種 1〜10・露・普）：貴族の LandRent・歳入の基数÷名目 GDP')
        for n in NAT:
            def tot(D, col):
                return sum(fl(D[(s, n, q)][col]) for s in SEEDS for q in QS if (s, n, q) in D)
            a, b = tot(A, 'landRentActualNoblePerQuarter'), tot(B, 'landRentActualNoblePerQuarter')
            say(f'{n}\t貴族の LandRent 新÷旧 {a / b if b > 0 else float("nan"):.4f}')
        for n in NAT:
            rr = {}
            for lab, src in (('新', arm), ('旧', base)):
                xs = []
                for s in SEEDS:
                    try:
                        ns, _ = rd(f'{src}/nation_s{s}.tsv')
                    except FileNotFoundError:
                        continue
                    for r in ns:
                        if r['nation'] == n and r['quarter'] in QS:
                            g = fl(r['nominalGdpPerYear']); v = fl(r['revenueBaseMeanPerMonth'])
                            if g > 0:
                                xs.append(12 * v / g)
                rr[lab] = st.fmean(xs) if xs else float('nan')
            say(f'{n}\t歳入の基数×12÷名目 GDP（q0〜q11 の平均）新 {rr["新"]:.4f} 旧 {rr["旧"]:.4f} 新÷旧 {rr["新"] / rr["旧"] if rr["旧"] > 0 else float("nan"):.4f}')
    allok = all(ok.values())
    say('判定: ' + ('緑' if allok else '赤'))
    open(out, 'wb').write(('\n'.join(L) + '\n').encode('utf-8'))
    print('\n'.join(L))
    return 0 if allok else 1


if __name__ == '__main__':
    if sys.argv[1] == 'paper':
        paper(sys.argv[2])
    else:
        sys.exit(gates(sys.argv[2], sys.argv[3], sys.argv[4] if len(sys.argv) > 4 else None))
