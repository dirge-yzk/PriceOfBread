# (1429) c153nx 腕の門と記録の台本（生の wexit_s と wars_s を読むだけ・エンジンと係数を読まない）。
# 使い方: python nx_gate.py <生のフォルダ> [種の数=10]
# 1 本の戦争＝wexit_s の同じ (powerIdA,powerIdB) で月が 1 ずつ続く行。終わり＝その戦争の最後の行で、表の最後の月より前。
# 崩壊の側＝exh≥cut（列から台本が組む）。接触なし＝cumPushA＋cumPushB≤1e-12。
# G1（門）：母集合（崩壊の側あり かつ 接触なし・表の最後の月を除く）の月にその戦争が終わった割合＝10/10 種で 1。種ごとの終わった戦争の数（下限 1・0 なら理由を書く）。
# G2（門）：崩壊の側あり かつ 接触あり の行で imposeScore が ±1.5×Clamp01(2×勝ち側の累計÷(両側の累計＋1e-12)) と |差|≤1e-8 で一致しない行の数＝0。
#   敗者：片側だけ崩壊ならその側・両側なら厭戦の大きい側・等しければ両方の候補のどちらか（同点の数は記録）。
# 記録：G2b 母集合の imposeScore の |x|≤1e-8 の割合・0＜和≤1e-12 の行・G3 母集合の外で旧の述語が全て 0 なのに終わった月・
#   表の最後まで続いた接触なしの戦争・60 か月以上の接触なしの戦争（最後の行の和が 0）・母集合の内戦・wars_s の白紙（verdictCode 0）。
import sys, os, collections

TOL = 1e-8
CS = 1.5

def load(p):
    with open(p, 'rb') as f:
        lines = f.read().decode('utf-8').splitlines()
    h = lines[0].split('\t'); ix = {n: i for i, n in enumerate(h)}
    rows = [l.split('\t') for l in lines[1:]]
    return ix, [r for r in rows if len(r) == len(h)], len(rows)

def runs(ix, rows):
    by = collections.defaultdict(list)
    for r in rows:
        by[(r[ix['powerIdA']], r[ix['powerIdB']])].append(r)
    out = []
    for lst in by.values():
        lst.sort(key=lambda r: int(r[ix['month']]))
        cur = [lst[0]]
        for r in lst[1:]:
            if int(r[ix['month']]) == int(cur[-1][ix['month']]) + 1: cur.append(r)
            else: out.append(cur); cur = [r]
        out.append(cur)
    return out

def main():
    d = sys.argv[1]; ns = int(sys.argv[2]) if len(sys.argv) > 2 else 10
    need = ['exhA', 'exhB', 'cutA', 'cutB', 'cumPushA', 'cumPushB', 'imposeScore']
    g1ok = 0; g2bad = 0; tot = collections.Counter()
    print('seed\tpopRows\tpopEnded\tG1\tpopWarsEnded\tpopCivil\tG2rows\tG2bad\tties\tG2bZero\ttinyPush\tG3\tcontNoContactEnd\tnoContactGe60\tblankWars\tbadRows')
    for s in range(1, ns + 1):
        p = os.path.join(d, f'wexit_s{s}.tsv')
        if not os.path.exists(p): print(f'{s}\tMISSING'); continue
        ix, rows, nraw = load(p)
        if any(n not in ix for n in need): print(f'{s}\tNOCOLS'); continue
        f = lambda r, n: float(r[ix[n]])
        lastm = max(int(r[ix['month']]) for r in rows)
        popRows = popEnded = popWars = popCivil = g2rows = bad = ties = zero = tiny = g3 = cont = ge60 = 0
        for run in runs(ix, rows):
            hit = False
            for j, r in enumerate(run):
                colA = f(r, 'exhA') >= f(r, 'cutA'); colB = f(r, 'exhB') >= f(r, 'cutB')
                ps = f(r, 'cumPushA') + f(r, 'cumPushB')
                ends = j == len(run) - 1 and int(r[ix['month']]) < lastm
                oldEnd = r[ix['oldDecisiveFlag']] == '1' or r[ix['oldWearyFlag']] == '1' or r[ix['forcedFlag']] == '1'
                if (colA or colB) and ps <= 1e-12:
                    if 0 < ps: tiny += 1
                    if int(r[ix['month']]) >= lastm: continue
                    popRows += 1
                    if abs(f(r, 'imposeScore')) <= TOL: zero += 1
                    if ends:
                        popEnded += 1
                        if not hit:
                            popWars += 1
                            if r[ix['civilFlag']] == '1': popCivil += 1
                    hit = True
                    continue
                if ends and not oldEnd: g3 += 1
                if (colA or colB) and ps > 1e-12 and r[ix['forcedFlag']] == '0':
                    g2rows += 1
                    pa, pb = f(r, 'cumPushA'), f(r, 'cumPushB')
                    def sc(aLoses):
                        win, lose = (pb, pa) if aLoses else (pa, pb)
                        reach = max(0.0, min(1.0, 2.0 * win / (win + lose + 1e-12)))
                        return -CS * reach if aLoses else CS * reach
                    if colA and colB:
                        ea, eb = f(r, 'exhA'), f(r, 'exhB')
                        cands = [ea > eb] if ea != eb else [True, False]
                        if ea == eb: ties += 1
                    else:
                        cands = [colA]
                    got = f(r, 'imposeScore')
                    if not any(abs(got - sc(c)) <= TOL for c in cands): bad += 1
            last = run[-1]
            if f(last, 'cumPushA') + f(last, 'cumPushB') <= 1e-12:
                if int(last[ix['month']]) == lastm: cont += 1
                if len(run) >= 60: ge60 += 1
        blank = 0
        wp = os.path.join(d, f'wars_s{s}.tsv')
        if os.path.exists(wp):
            wix, wrows, _ = load(wp)
            blank = sum(1 for r in wrows if r[wix['verdictCode']] == '0')
        g1 = popEnded / popRows if popRows else float('nan')
        if popRows and popEnded == popRows: g1ok += 1
        g2bad += bad
        print(f'{s}\t{popRows}\t{popEnded}\t{g1:.4f}\t{popWars}\t{popCivil}\t{g2rows}\t{bad}\t{ties}\t{zero}\t{tiny}\t{g3}\t{cont}\t{ge60}\t{blank}\t{nraw - len(rows)}')
        for k, v in (('popRows', popRows), ('popEnded', popEnded), ('popWars', popWars), ('popCivil', popCivil), ('G2rows', g2rows), ('ties', ties), ('G2bZero', zero), ('tiny', tiny), ('G3', g3), ('cont', cont), ('ge60', ge60), ('blank', blank)):
            tot[k] += v
    print('total\t' + '\t'.join(f'{k}={v}' for k, v in tot.items()))
    print(f'G1 {"PASS" if g1ok == ns else "FAIL"}（割合 1 の種 {g1ok}/{ns}）')
    print(f'G2 {"PASS" if g2bad == 0 else "FAIL"}（一致しない行 {g2bad}）')
    sys.exit(0 if (g1ok == ns and g2bad == 0) else 1)

if __name__ == '__main__':
    main()
