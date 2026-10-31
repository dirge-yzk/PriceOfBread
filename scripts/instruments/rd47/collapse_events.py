#!/usr/bin/env python
"""(698) rd47 ②：英仏普（ほか --keys の国）の〈本国の崩れ〉の事象の表（読むだけ・C# は変えない・既存の列だけで読む）。
使い方：python -X utf8 -I collapse_events.py <腕名>=<計器の出力フォルダ>[:<種の起点>] ... [--keys イギリス,フランス,プロイセン]
崩れの定義＝(693) home_break.py の〈E開〉と同じ：本国格の州（Core/Integrated）の人口 P か州の数 R が系列の最初の値の 0.5 倍を割る、または系列が最後の四半期より前に途切れる。
  国の鍵＝nationUid（nation_s）・名札＝系列の最初の名前・開幕（quarter 0）に居た国だけ。
出力（TSV）：
  C 行＝崩れた種ごとに：最初に割った四半期と年・割ったもの（P/R/終わり）・開幕の P0/R0・その時点の P/R・
       分解（開幕からその四半期まで）：
         lostK  ＝開幕の本国格の州のうち他の国へ移った州の、移る直前の四半期の人口の和（移った州の数 lostN）
         lostNew＝そのうち受け手が〈移る前後 4 四半期の内に nation_s に初めて現れた国〉（分離独立・内戦の建国・蜂起）の州の数
         lostOld＝受け手が既に居た国（講和の割譲・征服・併合）の州の数
         demoK  ＝持ち続けた州（開幕から崩れの四半期まで本国格のまま持った州）の人口の変化（＋は増）
         migOutK・migInK＝持ち続けた州の州の間の移民・越境の流出と流入の累計（region_s の四半期の流れの和・入植は除く）
         epiK   ＝持ち続けた州の疫病の死（epi_s の deadK の和）
         cw     ＝その国の内戦の月（nation_s の civilWarMonths の和・開幕から崩れまで）
         war    ＝その国の対外戦争の月（atWarMonths の和）
         recv   ＝受け手の国の名札と州の数（多い順に 4 つ）
       戻ったか：崩れの後、P が開幕の 0.5 倍以上に戻った最初の年（戻らなければ —）。
         birthsK_N ほか 5 列＝pop_s の N 行（国全体＝植民地込み）の出生・平常の死・飢えの死・その他の死・戦死の、開幕から崩れの四半期までの和（本国だけには分けられない）。
  S 行＝腕ごと・国ごとの要約（崩れた種の数・主な型の件数）。
型（AI の読み・推）：
  人口＝P で割り、失った州の人口 lostK が本国の人口の落ち（P0−P）の半分に届かない／建国＝それ以外で lostNew が lostN の半分以上（分離独立・内戦の側の建国）／割譲＝それ以外（講和の割譲・征服・併合）／途切れ＝系列が終わった。
限界の申告：
  * region_s は四半期の断面＝四半期の中の 2 回の移動は見えない。移った先は次に現れた四半期の持ち主。
  * 持ち続けた州の〈人口の変化〉は出生・死亡（飢え・疫病・戦死）・移民の合計＝epiK と migOut/In 以外の残りは出生−死亡（飢えの死を分ける列は region_s に無い）。
  * epi_s は計器の 9 国以外の州も含む（州の名で引く）。epi_s の州の名と region_s の州の名が一致することを最後に数える。
"""
import csv, os, sys
from collections import defaultdict
csv.field_size_limit(10**9)
SUST = {}   # (710) rd48 R6：(フォルダ, 種) -> [(国の名, 最初に割った四半期, 最長の続いた四半期の数, その始まりの四半期, 途切れたか)]


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


def seed(d, s, keys):
    # nation_s：(q, nationIndex) -> uid・uid -> 系列
    uid_of = {}; first_q = {}; label = {}; name_at = {}; cw = defaultdict(lambda: defaultdict(float)); war = defaultdict(lambda: defaultdict(float)); last_q = 0
    for ix, x in rows(os.path.join(d, f'nation_s{s}.tsv')):
        q = int(x[ix['quarter']]); u = x[ix['nationUid']]; last_q = max(last_q, q)
        uid_of[(q, int(x[ix['nationIndex']]))] = u
        if u not in first_q:
            first_q[u] = q; label[u] = x[ix['nation']]
        cw[u][q] = fl(x[ix['civilWarMonths']]); war[u][q] = fl(x[ix['atWarMonths']])
        name_at[(u, q)] = x[ix['nation']]
    # region_s：州ごとの持ち主・格・人口・流れ
    own = defaultdict(dict)   # region -> q -> (uid, tier, pop, migOut, migIn)
    natq = defaultdict(lambda: [0.0, 0.0, 0.0])   # (uid, q) -> [植民地格の人口, 全州の人口, 全州の農村人口]
    for ix, x in rows(os.path.join(d, f'region_s{s}.tsv')):
        q = int(x[ix['quarter']]); u = uid_of.get((q, int(x[ix['nationIndex']])), '?')
        mo = fl(x[ix['migrationOutKPerQuarter']]) + fl(x[ix['crossBorderOutKPerQuarter']])
        mi = fl(x[ix['migrationInKPerQuarter']]) + fl(x[ix['crossBorderInKPerQuarter']])
        own[x[ix['region']]][q] = (u, x[ix['tier']], fl(x[ix['populationK']]), mo, mi)
        a = natq[(u, q)]; pk = fl(x[ix['populationK']]); a[1] += pk; a[2] += fl(x[ix['ruralPopulationK']])
        if x[ix['tier']] == 'Colonial': a[0] += pk
    anc = {}   # (国の名, q) -> (capacity, capBase, arableSum)
    for ix, x in rows(os.path.join(d, f'anchor_s{s}.tsv')):
        anc[(x[ix['nation']], int(x[ix['quarter']]))] = (fl(x[ix['capacity']]), fl(x[ix['capBase']]), fl(x[ix['arableSum']]))
    epi = defaultdict(lambda: defaultdict(float)); epi_names = set()
    for ix, x in rows(os.path.join(d, f'epi_s{s}.tsv')):
        epi[x[ix['region']]][int(x[ix['quarter']])] += fl(x[ix['deadK']]); epi_names.add(x[ix['region']])
    # 国ごとの本国の系列
    home = defaultdict(lambda: defaultdict(lambda: [0.0, 0]))
    for rg, qs in own.items():
        for q, (u, t, p, _, _) in qs.items():
            if t != 'Colonial':
                a = home[u][q]; a[0] += p; a[1] += 1
    out = []
    sust = SUST.setdefault((d, s), [])
    for u, lab in label.items():
        if first_q[u] != 0 or lab not in keys:
            continue
        ser = sorted(home[u].items())
        if not ser:
            continue
        P0, R0 = ser[0][1]
        # (710) R6：半分を割った状態（P＜0.5×P0 か R＜0.5×R0）の最長の連続（系列の連続した四半期）・途切れ（既存の C 行の計算は変えない）
        run = best = 0; bstart = None; rstart = None; prevq = None; firstq = None
        for q, (P, R) in ser:
            below = P < 0.5 * P0 or R < 0.5 * R0
            if below and firstq is None: firstq = q
            if below and run > 0 and prevq is not None and q == prevq + 1: run += 1
            elif below: run = 1; rstart = q
            else: run = 0
            if run > best: best = run; bstart = rstart
            prevq = q
        sust.append((lab, firstq, best, bstart, max(q for q, _ in ser) < last_q - 1))
        qc = None; why = None
        for q, (P, R) in ser:
            if P < 0.5 * P0:
                qc, why = q, 'P'; break
            if R < 0.5 * R0:
                qc, why = q, 'R'; break
        endq = max(q for q, _ in ser)
        if qc is None and endq < last_q - 1:
            qc, why = endq, '終わり'
        if qc is None:
            continue
        Pc, Rc = home[u][qc] if qc in home[u] else (0.0, 0)
        # 開幕の本国格の州
        regs0 = [rg for rg, qs in own.items() if 0 in qs and qs[0][0] == u and qs[0][1] != 'Colonial']
        lostK = 0.0; lostN = 0; lostNew = 0; lostOld = 0; recv = defaultdict(int)
        demo = 0.0; mo = 0.0; mi = 0.0; ep = 0.0
        for rg in regs0:
            qs = own[rg]; moved = None
            for q in sorted(qs):
                if q > qc:
                    break
                if qs[q][0] != u:
                    moved = q; break
            if moved is not None:
                prev = max([q for q in qs if q < moved], default=0)
                lostK += qs[prev][2]; lostN += 1
                rv = qs[moved][0]
                if abs(first_q.get(rv, -99) - moved) <= 4:
                    lostNew += 1
                else:
                    lostOld += 1
                recv[label.get(rv, rv)] += 1
            else:
                if qc in qs and qs[qc][1] != 'Colonial':
                    demo += qs[qc][2] - qs[0][2]
                    for q in range(1, qc + 1):
                        if q in qs:
                            mo += qs[q][3]; mi += qs[q][4]
                        ep += epi[rg].get(q, 0.0)
        cwm = sum(v for q, v in cw[u].items() if q <= qc); wm = sum(v for q, v in war[u].items() if q <= qc)
        back = '—'
        for q, (P, R) in ser:
            if q > qc and P >= 0.5 * P0 and R >= 0.5 * R0:
                back = str(1783 + q // 4); break
        drop = P0 - Pc
        if why == '終わり':
            typ = '途切れ'
        elif why == 'P' and not (lostK >= 0.5 * drop):
            typ = '人口'
        elif lostNew * 2 >= lostN:
            typ = '建国'
        else:
            typ = '割譲'
        rv = ','.join(f'{k}:{v}' for k, v in sorted(recv.items(), key=lambda t: -t[1])[:4])
        # ★追加（rd47）：植民地格の人口（開幕・崩れまでの最大・崩れの四半期）・扶養力（anchor_s＝国全体：capacity・capBase・arableSum・capacity÷capBase）・都市の割合（1−Σ農村÷Σ人口・国全体の全州）
        cp0 = natq[(u, 0)][0]; cpc = natq[(u, qc)][0] if (u, qc) in natq else float('nan')
        cpmax = max([natq[(u, q)][0] for q in range(0, qc + 1) if (u, q) in natq], default=float('nan'))
        def capt(q):
            k = anc.get((name_at.get((u, q), lab), q))
            if not k: return (float('nan'),) * 4
            return k + (k[0] / k[1] if k[1] > 0 else float('nan'),)
        def urb(q):
            a = natq.get((u, q)); return 1 - a[2] / a[1] if a and a[1] > 0 else float('nan')
        out.append((lab, s, qc, 1783 + qc // 4, why, typ, P0, R0, Pc, Rc, lostK, lostN, lostNew, lostOld, demo, mo, mi, ep, cwm, wm, rv, back)
                   + (cp0, cpmax, cpc) + capt(0) + capt(qc) + (urb(0), urb(qc)))
    matched = sum(1 for n in epi_names if n in own)
    # pop_s の N 行（国全体＝植民地込み）：開幕から崩れの四半期までの出生・死の内訳の和
    want = {(r[0], r[2]) for r in out}
    lab_uid = {}
    for u, lab in label.items():
        if first_q[u] == 0: lab_uid[lab] = u
    sums = defaultdict(lambda: [0.0] * 5)
    for ix, x in rows(os.path.join(d, f'pop_s{s}.tsv')):
        if x[ix['scope']] != 'N': continue
        u = x[ix['nationUid']]; q = int(x[ix['quarter']])
        for (lab, qc) in want:
            if lab_uid.get(lab) == u and q <= qc:
                a = sums[(lab, qc)]
                for k, c in enumerate(('birthsK', 'deathsNormalK', 'deathsFamineK', 'deathsOtherK', 'warDeadK')):
                    v = fl(x[ix[c]]); a[k] += v if v == v else 0.0
    out = [r[:22] + tuple(sums[(r[0], r[2])]) + r[22:] for r in out]
    return out, (matched, len(epi_names))


def main():
    arms = []; keys = ['イギリス', 'フランス', 'プロイセン']
    a = sys.argv[1:]; i = 0
    while i < len(a):
        if a[i] == '--keys':
            keys = a[i + 1].split(','); i += 2; continue
        name, rest = a[i].split('=', 1)
        d, s0 = (rest.rsplit(':', 1) if ':' in rest[2:] else (rest, '1'))
        arms.append((name, d, int(s0))); i += 1
    print('kind\tarm\tnation\tseed\tquarter\tyear\twhy\ttype\tP0\tR0\tP\tR\tlostK\tlostN\tlostNew\tlostOld\tdemoK\tmigOutK\tmigInK\tepiK\tcwMonths\twarMonths\trecv\tback\tbirthsK_N\tdeathsNormalK_N\tdeathsFamineK_N\tdeathsOtherK_N\twarDeadK_N\tcolPop0\tcolPopMax\tcolPopC\tcap0\tcapBase0\tarable0\tcapRatio0\tcapC\tcapBaseC\tarableC\tcapRatioC\turban0\turbanC')
    summ = []
    for name, d, s0 in arms:
        agg = defaultdict(lambda: defaultdict(int)); chk = [0, 0]
        for s in range(s0, s0 + 10):
            out, (m, n) = seed(d, s, keys)
            chk[0] += m; chk[1] += n
            for r in out:
                lab = r[0]; agg[lab]['n'] += 1; agg[lab][r[5]] += 1
                if r[3] <= 1825: agg[lab]['<=1825'] += 1
                print('C\t%s\t%s\t%d\t%d\t%d\t%s\t%s\t%.0f\t%d\t%.0f\t%d\t%.0f\t%d\t%d\t%d\t%.0f\t%.0f\t%.0f\t%.0f\t%.0f\t%.0f\t%s\t%s\t%.0f\t%.0f\t%.0f\t%.0f\t%.0f' % ((name,) + r[:27]) + ''.join('\t%.0f' % v for v in r[27:30]) + ''.join('\t%.0f\t%.0f\t%.0f\t%.3f' % tuple(r[i:i + 4]) for i in (30, 34)) + '\t%.3f\t%.3f' % r[38:40])
        for k in keys:
            g = agg[k]
            summ.append('S\t%s\t%s\t崩れ %d/10・型 建国 %d・割譲 %d・人口 %d・途切れ %d・1825 年までに %d\t（epi_s の州の名が region_s に在る %d/%d）' % (name, k, g['n'], g['建国'], g['割譲'], g['人口'], g['途切れ'], g['<=1825'], chk[0], chk[1]))
    print('\n'.join(summ))
    # (710) rd48 R6：D 行＝種ごとの〈半分を割った状態の最長の連続〉・R 行＝4 四半期以上続いた（か途切れた）種の数（既存の C 行・S 行は 1 バイトも変えない）
    print('kind\tarm\tnation\tseed\tfirstBelowYear\tlongestRunQ\trunStartYear\ttruncated\tsustained4')
    for name, d, s0 in arms:
        cnt = defaultdict(lambda: [0, 0, 0])
        for s in range(s0, s0 + 10):
            for lab, fq, best, bs, tr in SUST.get((d, s), []):
                ok = tr or best >= 4
                c = cnt[lab]; c[0] += fq is not None or tr; c[1] += ok; c[2] += best >= 4
                if fq is not None or tr:
                    print('D\t%s\t%s\t%d\t%s\t%d\t%s\t%d\t%d' % (name, lab, s, 1783 + fq // 4 if fq is not None else '—', best, 1783 + bs // 4 if bs is not None else '—', tr, ok))
        for k in keys:
            c = cnt[k]
            print('R\t%s\t%s\t割った（か途切れた）%d/10・4 四半期以上続いた（か途切れた）%d/10・続いた（途切れを除く）%d/10' % (name, k, c[0], c[1], c[2]))


main()
