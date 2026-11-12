# (1064) c101vs 宗主と属国の輪を切る：紙の台本と門の集計（エンジンの関数を呼ばない・表だけを読む）。
#   python gate_vs.py paper <基線の生> <N1 の生> <N2 の生>
#       G2 の帯＝invade_s の〈targetIsNation=1 かつ submitted=1〉（Subjugate の属国化の経路の件数）を 10 種で足し、基線と N1・N2 の幅を出す。
#   python gate_vs.py gate <旗 0 の生（dep_s あり）> <腕 B の生（dep_s あり）> <基線の生> <N1 の生> <N2 の生>
#       G1・G1b・G2・G3 と記録（呼び口ごとの件数・交戦中の呼び）を出す。
# dep_s の列は InstrumentsDep.cs の DepHeaderText（kindCode 1＝呼び 1 件・2＝試合の終わりの全リンクの行）。
# 限界（自己申告）：①輪はリンクを足す SetDependency の呼びでしか生まれない（_dependency に足すのは Diplomacy.cs の 1 行だけ・外す側は
#   RemoveDependency・RemovePower・SetDependency の中の外し）＝〈全ての呼びの直後の輪の数〉の最大が 0 なら、年末を含む全ての時点で 0。
#   年末の行は取らない（呼び口が Close の 1 行だけ＝年の変わり目の引き金が無い）。②N1・N2 に dep_s は無い＝G2 の幅は invade_s の
#   Subjugate の経路だけ（講和の経路〔属国化・単州の従属化・引き継ぎ〕の件数は旗 0 と腕 B の比を記録するだけ）。
import csv, glob, os, sys

def rd(p):
    with open(p, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t')
        h = next(r)
        return h, [row for row in r]

def seeds(d, t):
    out = []
    for p in glob.glob(os.path.join(d, f'{t}_s*.tsv')):
        b = os.path.basename(p)[len(t) + 2:-4]
        if b.isdigit(): out.append(int(b))
    return sorted(out)

def invade_sub(d):
    tot = 0; per = {}
    for s in seeds(d, 'invade'):
        h, rows = rd(os.path.join(d, f'invade_s{s}.tsv'))
        i, j = h.index('targetIsNation'), h.index('submitted')
        n = sum(1 for r in rows if r[i] == '1' and r[j] == '1')
        per[s] = n; tot += n
    return tot, per

def band(base, n1, n2):
    vals = [base, n1, n2]; w = max(vals) - min(vals)
    return min(vals) - w, max(vals) + w, w

def dep(d):
    res = {}
    for s in seeds(d, 'dep'):
        h, rows = rd(os.path.join(d, f'dep_s{s}.tsv'))
        res[s] = (h, rows)
    return res

def col(h, r, n): return r[h.index(n)]

def summarize(dd):
    out = {}
    for s, (h, rows) in dd.items():
        calls = [r for r in rows if col(h, r, 'kindCode') == '1']
        fin = [r for r in rows if col(h, r, 'kindCode') == '2']
        cyc = [r for r in calls if col(h, r, 'cycleFlag') == '1']
        cut_on_cyc = sum(1 for r in cyc if col(h, r, 'cutSuzerain') != '')
        cut_off_cyc = sum(1 for r in calls if col(h, r, 'cycleFlag') == '0' and col(h, r, 'cutSuzerain') != '')
        maxafter = max([int(col(h, r, 'cyclesAfterCount')) for r in calls] + [0])
        fincyc = int(col(h, fin[-1], 'cyclesAfterCount')) if fin else -1
        bycaller = {}
        for r in calls:
            k = col(h, r, 'callerMember') + ':' + ('take' if col(h, r, 'priorSuzerainOfDependent') != '' else 'new')
            bycaller[k] = bycaller.get(k, 0) + 1
        war = sum(1 for r in calls if col(h, r, 'atWarFlag') == '1')
        cap = sum(1 for r in calls if col(h, r, 'capHitFlag') == '1')
        out[s] = dict(calls=len(calls), cyc=len(cyc), cut_on=cut_on_cyc, cut_off=cut_off_cyc, maxafter=maxafter, fincyc=fincyc,
                      bycaller=bycaller, war=war, cap=cap, cycrows=[(col(h, r, 'year'), col(h, r, 'callerMember'), col(h, r, 'callerLine'),
                                                                     col(h, r, 'suzerain'), col(h, r, 'dependent'), col(h, r, 'chainLenIndex')) for r in cyc])
    return out

def g3(d0, d1):
    # 輪の印が 0 の呼びは旗 0 と同じ＝旗 0 の最初の輪の行より前の呼びの行が、旗 1 でも同じ位置にバイトで同じ。
    res = {}
    for s in sorted(set(d0) & set(d1)):
        h0, r0 = d0[s]; h1, r1 = d1[s]
        c0 = [r for r in r0 if col(h0, r, 'kindCode') == '1']; c1 = [r for r in r1 if col(h1, r, 'kindCode') == '1']
        k = next((i for i, r in enumerate(c0) if col(h0, r, 'cycleFlag') == '1'), len(c0))
        same = h0 == h1 and len(c1) >= k and all(c0[i] == c1[i] for i in range(k))
        res[s] = (k, same)
    return res

def main():
    mode = sys.argv[1]
    if mode == 'paper':
        b, n1, n2 = (invade_sub(x) for x in sys.argv[2:5])
        lo, hi, w = band(b[0], n1[0], n2[0])
        print(f'G2 帯（invade_s の Subjugate の属国化・10 種の合計）：基線 {b[0]}・N1 {n1[0]}・N2 {n2[0]}＝幅 {w}＝[{lo}, {hi}]')
        print('種ごと（基線）：' + ' '.join(f's{s}={v}' for s, v in sorted(b[1].items())))
        print(f'旗 0 の盤面で G2 が通るか：基線の値 {b[0]} は帯の中＝{lo <= b[0] <= hi}（形から通る門＝働いた証拠にしない）')
        return
    d0, d1 = dep(sys.argv[2]), dep(sys.argv[3])
    s0, s1 = summarize(d0), summarize(d1)
    for name, sm in (('旗 0', s0), ('旗 1（腕 B）', s1)):
        print(f'== {name}：種 {len(sm)}')
        print('  呼び ' + ' '.join(f's{s}={v["calls"]}' for s, v in sorted(sm.items())))
        print('  輪の印の呼び ' + ' '.join(f's{s}={v["cyc"]}' for s, v in sorted(sm.items())) + f'・合計 {sum(v["cyc"] for v in sm.values())}')
        print('  呼びの直後の輪の数の最大 ' + ' '.join(f's{s}={v["maxafter"]}' for s, v in sorted(sm.items())))
        print('  終わりの輪の数 ' + ' '.join(f's{s}={v["fincyc"]}' for s, v in sorted(sm.items())))
        print('  輪の印で切った ' + str(sum(v['cut_on'] for v in sm.values())) + '・輪の印 0 で切った ' + str(sum(v['cut_off'] for v in sm.values()))
              + '・打ち切りに当たった ' + str(sum(v['cap'] for v in sm.values())) + '・交戦中の呼び ' + str(sum(v['war'] for v in sm.values())))
        agg = {}
        for v in sm.values():
            for k, n in v['bycaller'].items(): agg[k] = agg.get(k, 0) + n
        print('  呼び口 ' + ' '.join(f'{k}={n}' for k, n in sorted(agg.items())))
        for s, v in sorted(sm.items()):
            for row in v['cycrows']: print(f'  輪 s{s} ' + ' '.join(row))
    tot0 = sum(v['cyc'] for v in s0.values())
    g1 = all(v['maxafter'] == 0 and v['fincyc'] == 0 for v in s1.values()) and len(s1) == 10
    g1_old = any(v['maxafter'] > 0 for v in s0.values())
    g1b = sum(v['cut_on'] for v in s1.values()) == sum(v['cyc'] for v in s1.values()) and sum(v['cut_off'] for v in s1.values()) == 0
    print(f'G1 働いた（旗 1 の全ての呼びの直後と終わりの輪の数＝0・10 種）：{"通る" if g1 else "落ちる"}・旗 0 で落ちるか：{"落ちる" if g1_old else "落ちない（輪 0 件）"}・旗 0 の輪の印の呼び {tot0}')
    print(f'G1b 消しすぎない（旗 1：輪の印の呼びごとに切ったリンクが 1 本・輪の印 0 の呼びで切ったリンク 0）：{"通る" if g1b else "落ちる"}')
    r3 = g3(d0, d1)
    print('G3（輪の印 0 の呼びは最初の輪まで同じ）：' + ' '.join(f's{s}:{k}行{"同" if ok else "違"}' for s, (k, ok) in sorted(r3.items()))
          + f'＝{"通る" if all(ok for _, ok in r3.values()) else "落ちる"}')
    if len(sys.argv) >= 7:
        b, n1, n2 = (invade_sub(x) for x in sys.argv[4:7])
        a = invade_sub(sys.argv[3])
        lo, hi, w = band(b[0], n1[0], n2[0])
        print(f'G2（invade_s の Subjugate の属国化・10 種の合計）：腕 B {a[0]}・帯 [{lo}, {hi}]（基線 {b[0]}・N1 {n1[0]}・N2 {n2[0]}）＝{"通る" if lo <= a[0] <= hi else "落ちる"}')
        c0 = sum(v['calls'] for v in s0.values()); c1 = sum(v['calls'] for v in s1.values())
        print(f'記録：dep_s の呼びの合計 旗 0 {c0}・腕 B {c1}・新÷旧 {c1 / c0 if c0 else float("nan"):.3f}')

if __name__ == '__main__':
    main()
