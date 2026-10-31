#!/usr/bin/env python
"""(710) rd48 ③：1 国（既定 イギリス）が開幕に持つ植民地格の州を、いつ・誰へ・どの経路で失うかの事象の表（読むだけ・C# は変えない・既存の列だけで読む）。
使い方：python -X utf8 -I colony_events.py <腕名>=<計器の出力 ins フォルダ>[:<種の起点>] ... [--nation イギリス] [--top 6] [--home]
  --home＝開幕の本国格の州（Colonial 以外）を数える（仏の本国の割譲の帰属に使った・H 行の〈植民地の人口〉の列は読まない）。
読む列：region_s（quarter・nationIndex・region・tier・populationK）・nation_s（quarter・nationIndex・nationUid・nation）・wars_s（startYear・civilWarFlag・sideA・sideB・months）・
        score_s（quarter・nation・scoreShare・share1Soldiers・share2MilitarySpend）・stock_s（populationK・homePopulationK＝colony_loss.py と同じ〈植民地の人口〉）。
出力：
  E 行＝事象（同じ四半期・同じ受け手でまとめる）：腕・種・年・受け手・受け手の型（新＝移る前後 4 四半期の内に nation_s に初めて現れた国＝独立・内戦の建国／既＝既に居た国＝講和の割譲・征服）・
        州の数・移る直前の四半期の人口（千人）・開幕の植民地の人口に対する累計の割合・合う戦争（wars_s：両者が sideA/sideB・開戦の年≦事象の年≦開戦の年＋月数/12＋1）・
        開戦の年の国力の取り分（score_s の scoreShare＝世界の総量の割合）：失う側・受け手・兵士の取り分・軍事費の取り分。
  H 行＝種ごとの要約：植民地の人口（stock_s の populationK−homePopulationK）が開幕の半分を最初に割った年（colony_loss.py と同じ）・州を失う累計が開幕の植民地の人口の半分に届いた年・
        その年までの受け手の上位（州の数・人口）・新／既の内訳。
  S 行＝腕ごとの要約（半分を割った年の中央・1820 年までの種の数・受け手の型の人口の割合）。
限界の申告：
  * region_s は四半期の断面＝四半期の中の 2 回の移動は見えない。移った先は次に現れた四半期の持ち主。開幕に植民地格だった州だけを数える（後で得た州・本国格の州は数えない）。
  * 戦争の照合は名前で引く（改名の国は合わない＝〈—〉）。内戦の建国の受け手は内戦の行（civilWarFlag=1）に当たる。
  * 割譲の経路（CedeTerritory の境界改定・戦利品・植民地格の州の複数割譲・独立の確定）は計器に列が無い＝受け手の型と戦争の有無から読む（推）。
"""
import csv, os, statistics, sys
from collections import defaultdict
csv.field_size_limit(10**9)


def rows(p):
    if not os.path.exists(p):
        return
    with open(p, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r)
        ix = {c: i for i, c in enumerate(h)}
        for x in r:
            if len(x) == len(h):
                yield ix, x


def fl(v):
    try:
        return float(v)
    except Exception:
        return float('nan')


def seed(d, s, nat, home=False):
    uid_of = {}; first_q = {}; label = {}; name_at = {}; last_q = 0
    for ix, x in rows(os.path.join(d, f'nation_s{s}.tsv')):
        q = int(x[ix['quarter']]); u = x[ix['nationUid']]; last_q = max(last_q, q)
        uid_of[(q, int(x[ix['nationIndex']]))] = u
        if u not in first_q:
            first_q[u] = q; label[u] = x[ix['nation']]
        name_at[(u, q)] = x[ix['nation']]
    me = next((u for u, lab in label.items() if lab == nat and first_q[u] == 0), None)
    if me is None:
        return None
    own = defaultdict(dict)
    for ix, x in rows(os.path.join(d, f'region_s{s}.tsv')):
        q = int(x[ix['quarter']]); u = uid_of.get((q, int(x[ix['nationIndex']])), '?')
        own[x[ix['region']]][q] = (u, x[ix['tier']], fl(x[ix['populationK']]))
    col0 = {rg: qs[0][2] for rg, qs in own.items() if 0 in qs and qs[0][0] == me and ((qs[0][1] != 'Colonial') if home else (qs[0][1] == 'Colonial'))}   # --home：開幕の本国格の州（Colonial 以外）
    C0 = sum(col0.values())
    wars = []
    for ix, x in rows(os.path.join(d, f'wars_s{s}.tsv')):
        wars.append((int(x[ix['startYear']]), x[ix['civilWarFlag']], x[ix['sideA']], x[ix['sideB']], fl(x[ix['months']])))
    score = {}
    for ix, x in rows(os.path.join(d, f'score_s{s}.tsv')):
        score[(x[ix['nation']], int(x[ix['year']]))] = (fl(x[ix['scoreShare']]), fl(x[ix['share1Soldiers']]), fl(x[ix['share2MilitarySpend']]))
    half_stock = None; c0s = None
    for ix, x in rows(os.path.join(d, f'stock_s{s}.tsv')):
        if x[ix['nation']] != nat:
            continue
        col = fl(x[ix['populationK']]) - fl(x[ix['homePopulationK']])
        if col != col:
            continue
        if c0s is None: c0s = col
        elif half_stock is None and col < 0.5 * c0s: half_stock = int(x[ix['year']])
    ev = defaultdict(lambda: [0, 0.0])   # (q, recvUid) -> [州の数, 人口]
    for rg in col0:
        qs = own[rg]; prev = 0
        for q in sorted(qs):
            if qs[q][0] != me:
                ev[(q, qs[q][0])][0] += 1; ev[(q, qs[q][0])][1] += qs[prev][2]
                break
            prev = q
    out = []; cum = 0.0; half_lost = None
    for (q, rv), (n, pk) in sorted(ev.items()):
        y = 1783 + q // 4; cum += pk
        rl = name_at.get((rv, q), label.get(rv, rv))
        typ = '新' if abs(first_q.get(rv, -99) - q) <= 4 else '既'
        w = None
        for (sy, cwf, a, b, mo) in wars:
            if {a, b} == {nat, rl} and sy <= y <= sy + mo / 12.0 + 1:
                w = (sy, cwf, mo); break
        if w:
            sm = score.get((nat, w[0]), (float('nan'),) * 3); sr = score.get((rl, w[0]), (float('nan'),) * 3)
        else:
            sm = sr = (float('nan'),) * 3
        out.append((s, y, rl, typ, n, pk, cum / C0 if C0 > 0 else float('nan'), w, sm, sr))
        if half_lost is None and C0 > 0 and cum >= 0.5 * C0: half_lost = y
    return out, half_stock, half_lost, C0, len(col0)


def main():
    a = sys.argv[1:]; nat = 'イギリス'; top = 6; arms = []; home = False
    i = 0
    while i < len(a):
        if a[i] == '--nation': nat = a[i + 1]; i += 2; continue
        if a[i] == '--top': top = int(a[i + 1]); i += 2; continue
        if a[i] == '--home': home = True; i += 1; continue
        name, rest = a[i].split('=', 1)
        d, s0 = (rest.rsplit(':', 1) if ':' in rest[2:] else (rest, '1'))
        arms.append((name, d, int(s0))); i += 1
    print(f'# {nat} の開幕の' + ('本国格の州（--home）' if home else '植民地格の州') + 'の喪失の事象（E＝事象・H＝種の要約・S＝腕の要約）・--home のとき H 行の〈植民地の人口〉の列は読まない（stock_s の植民地の量のまま）')
    print('kind\tarm\tseed\tyear\treceiver\trecvType\tregions\tpopK\tcumShareOfOpenColPop\twarStart\tcivilWar\twarMonths\tscoreShareLoser\tscoreShareRecv\tsoldiersShareLoser\tsoldiersShareRecv\tmilSpendShareLoser\tmilSpendShareRecv')
    summ = []
    for name, d, s0 in arms:
        hs = []; hl = []; typpop = defaultdict(float); tot = 0.0
        for s in range(s0, s0 + 10):
            r = seed(d, s, nat, home)
            if r is None:
                continue
            out, half_stock, half_lost, C0, n0 = r
            hs.append(half_stock); hl.append(half_lost)
            recv = defaultdict(lambda: [0, 0.0]); nt = defaultdict(float)
            for (s_, y, rl, typ, n, pk, cs, w, sm, sr) in out:
                ws = (str(w[0]), w[1], '%.0f' % w[2]) if w else ('—', '—', '—')
                print('E\t%s\t%d\t%d\t%s\t%s\t%d\t%.0f\t%.3f\t%s\t%s\t%s\t%.4f\t%.4f\t%.4f\t%.4f\t%.4f\t%.4f' % ((name, s, y, rl, typ, n, pk, cs) + ws + (sm[0], sr[0], sm[1], sr[1], sm[2], sr[2])))
                lim = half_lost if half_lost is not None else 9999
                if y <= lim:
                    recv[rl][0] += n; recv[rl][1] += pk; nt[typ] += pk
                    typpop[typ + ('・戦争あり' if w else '・戦争なし')] += pk; tot += pk
            rs = ','.join('%s:%d州/%.0f' % (k, v[0], v[1]) for k, v in sorted(recv.items(), key=lambda t: -t[1][1])[:top])
            print('H\t%s\t%d\t植民地の人口が半分を割った年 %s・州を失う累計が半分に届いた年 %s・開幕の植民地 %d 州/%.0f 千人・その年までの受け手 %s・新 %.0f／既 %.0f 千人'
                  % (name, s, half_stock or '—', half_lost or '—', n0, C0, rs, nt['新'], nt['既']))
        ys = [y for y in hs if y]; yl = [y for y in hl if y]
        summ.append('S\t%s\t植民地の人口が半分を割った種 %d/10・中央 %s・1820 年までに %d／州の累計が半分に届いた種 %d/10・中央 %s・1820 年までに %d／半分に届くまでの受け手の型（人口の割合）%s'
                    % (name, len(ys), statistics.median(ys) if ys else '—', sum(1 for y in ys if y <= 1820), len(yl), statistics.median(yl) if yl else '—', sum(1 for y in yl if y <= 1820),
                       '・'.join('%s %.2f' % (k, v / tot) for k, v in sorted(typpop.items(), key=lambda t: -t[1])) if tot > 0 else '—'))
    print('\n'.join(summ))


main()
