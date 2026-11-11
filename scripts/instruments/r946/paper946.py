# (946) c83hs 紙の台本：開幕の州の支配度の初期値（HeldSinceFromData・B14）を旗 1 にしたときの分離の時計を、
# 盤面 m933 の生（keep_933/{base_ins,n1,n2}/secession_s・cederule_s）で数える。エンジンは呼ばない。
# 使い方: python -X utf8 -I paper946.py <world.json> <keep_dir> <out_dir>
# 根拠（ソースの現物・d313515b）：
#   支配度の書き手は Politics.cs:1546〜1548 の 1 か所（d ← clamp01(d + (引き留め÷共有分母 − 圧力)×DominanceSpeed)）。
#   読み手（力学）は Politics.cs:1605 の判定（!(d>0) で分離独立の戦争へ）だけ。圧力・引き留め・共有分母は Dominance も Tier も読まない。
#   ⇒ 基線の marginIndex（holdShareIndex − pressureIndex）で初期値だけ替えて積分し直すのは、最初の分離の事象までは厳密。
# 限界（自己申告）：
#   ①secession_s は計器の選んだ 11 国だけ毎四半期・他の国は年の末（q%4==3）だけ＝ペルシャ（バクー）は年の分解能で、margin×4 で近似する。
#   ②最初の分離の事象の後（どこかの州が新たに分離した後）は世界が変わる＝その先の紙は厳密でない（10 年の窓で読む）。
#   ③自己点検＝前の行の dominanceShare から次の行を予言して表の値と比べる（列の読み違いを拾う・同語反復でない＝積分は台本が自分でする）。
import sys, os, json, statistics as st
from collections import defaultdict

SPEED = 0.025      # Coefficients.cs:2754 DominanceSpeed
MATURE = 30.0      # Coefficients.cs:2756 DominanceMatureYears
COREY = 150.0      # Coefficients.cs:3292 CoreHeldYears
START = 1783       # Calendar.cs:17
WIN = 40           # 10 年＝40 四半期

def clamp01(x):
    if not (x > 0): return 0.0
    return 1.0 if x > 1 else x

def load_targets(wp):
    w = json.load(open(wp, encoding='utf-8'))
    t = {}
    for n in w['nations']:
        for p in n['provinces']:
            if 'heldSince' in p:
                hs = p['heldSince']
                d0 = 1.0 if hs <= 0 else clamp01((START - hs) / MATURE)
                tier = 'Core' if (START - hs) >= COREY else 'Integrated'
                t[(n['name'], p['city'])] = dict(hs=hs, d0=d0, tier=tier, rule=p.get('rule', n.get('rule')))
    return t

def med(xs):
    xs = [x for x in xs if x is not None]
    return st.median(xs) if xs else float('nan')

def run_arm(arm_dir, targets, out):
    res = {}
    seeds = list(range(1, 11))
    chk_n = chk_bad = 0
    per = defaultdict(list)   # key -> list of dict per seed
    col_rows = []             # opening colonies
    sec_counts = []
    for s in seeds:
        fp = os.path.join(arm_dir, f'secession_s{s}.tsv')
        if not os.path.exists(fp): continue
        last = {}             # key -> (quarter, dominance)
        tser = defaultdict(list)  # target key -> [(q, margin, dom, go, popK, pressure, hold)]
        cols = {}             # opening colonial keys -> first row
        with open(fp, encoding='utf-8') as f:
            h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
            iq, inat, ireg, itier = ix['quarter'], ix['nation'], ix['region'], ix['tier']
            im, idom, igo, ipop = ix['marginIndex'], ix['dominanceShare'], ix['goFlag'], ix['popK']
            ipr, iho = ix['pressureIndex'], ix['holdShareIndex']
            for line in f:
                a = line.rstrip('\n').split('\t')
                q = int(a[iq]); key = (a[inat], a[ireg])
                try:
                    m = float(a[im]); d = float(a[idom])
                except ValueError:
                    continue
                pv = last.get(key)
                if pv is not None and pv[0] == q - 1:
                    pred = clamp01(pv[1] + m * SPEED); chk_n += 1
                    if abs(pred - d) > 1e-6: chk_bad += 1
                last[key] = (q, d)
                if key in targets and q < 4 * 60:
                    tser[key].append((q, m, d, a[igo] == '1', float(a[ipop]), float(a[ipr]), float(a[iho])))
                if q < 4 and a[itier] == 'Colonial' and key not in cols:
                    cols[key] = (q, d, m)
        for key, info in targets.items():
            ser = tser.get(key, [])
            if not ser:
                per[key].append(None); continue
            yearly = ser[0][0] == 3 and (len(ser) < 2 or ser[1][0] - ser[0][0] == 4)
            step = 4 if yearly else 1
            # 紙（a）：初期値 d0 から基線の margin で積分し直す
            d = info['d0']; hit = None; dmin = d; q0m = ser[0][1]
            negq = 0; mins = 0.0; cum = 0.0
            base_go = None
            for (q, m, dd, go, pk, pr, ho) in ser:
                if q >= WIN: break
                d = clamp01(d + m * SPEED * step)
                cum += m * SPEED * step; mins = min(mins, cum)
                if m < 0: negq += 1
                dmin = min(dmin, d)
                if hit is None and not (d > 0): hit = q
                if base_go is None and go: base_go = q
            per[key].append(dict(q0m=q0m, hit=hit, dmin=dmin, negq=negq, need=max(0.0, -mins), yearly=yearly,
                                 pop0=ser[0][4], pr0=ser[0][5], ho0=ser[0][6], base_go=base_go, first_q=ser[0][0], last_q=ser[-1][0]))
        # 開幕の植民地（四半期 0〜3 の最初の行が Colonial）
        for key, (q, d, m) in cols.items():
            col_rows.append((s, key, q, d, m))
        # cederule_s：分離の建国の件数（完全な事象の表）
        cp = os.path.join(arm_dir, f'cederule_s{s}.tsv')
        nsec = nsec10 = nsec1 = nsec_d0 = col_sec10 = 0; tsec = []
        with open(cp, encoding='utf-8') as f:
            h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
            for line in f:
                a = line.rstrip('\n').split('\t')
                if a[ix['how']] != 'Seceded': continue
                q = int(a[ix['quarter']]); nsec += 1
                if q < WIN: nsec10 += 1
                if q < 4: nsec1 += 1
                try:
                    if not (float(a[ix['dominanceBeforeShare']]) > 0): nsec_d0 += 1
                except ValueError: pass
                if q < WIN and a[ix['tierBefore']] == 'Colonial': col_sec10 += 1
                if (a[ix['sender']], a[ix['region']]) in targets: tsec.append((a[ix['region']], q))
        sec_counts.append(dict(s=s, nsec=nsec, nsec10=nsec10, nsec1=nsec1, nsec_d0=nsec_d0, col_sec10=col_sec10, tsec=tsec))
    return per, col_rows, sec_counts, chk_n, chk_bad

def main():
    wp, keep, outd = sys.argv[1], sys.argv[2], sys.argv[3]
    targets = load_targets(wp)
    summary = []
    for arm in ['base_ins', 'n1', 'n2']:
        per, col_rows, sec, chk_n, chk_bad = run_arm(os.path.join(keep, arm), targets, None)
        L = []
        L.append(f'# arm={arm}  自己点検（前の行から予言した支配度と表の値の差>1e-6）: {chk_bad}/{chk_n}')
        L.append('# 29 州：国\t州\theldSince\td0\tTier(旗1)\tRule[0]\t人口K(q0中央)\tq0のmargin(中央・最小)\t10年でmargin<0の四半期(中央)\t生き残りに要るd0(10年・最大)\t紙(a)で10年以内に0になる種\t紙(a)の10年の最小d(中央)\t基線で10年以内にgo\t行の分解能')
        for key, info in sorted(targets.items(), key=lambda kv: (kv[1]['d0'], kv[0])):
            xs = [x for x in per[key] if x is not None]
            if not xs:
                L.append(f'{key[0]}\t{key[1]}\t{info["hs"]}\t{info["d0"]:.3f}\t{info["tier"]}\t{info["rule"][0]}\t行なし'); continue
            hits = sum(1 for x in xs if x['hit'] is not None)
            bgo = sum(1 for x in xs if x['base_go'] is not None)
            L.append('\t'.join(map(str, [key[0], key[1], info['hs'], f'{info["d0"]:.3f}', info['tier'], info['rule'][0],
                f'{med([x["pop0"] for x in xs]):.1f}', f'{med([x["q0m"] for x in xs]):.3f}・{min(x["q0m"] for x in xs):.3f}',
                f'{med([x["negq"] for x in xs])}', f'{max(x["need"] for x in xs):.3f}', f'{hits}/{len(xs)}',
                f'{med([x["dmin"] for x in xs]):.3f}', f'{bgo}/{len(xs)}', '年' if xs[0]['yearly'] else '四半期'])))
        # 開幕の植民地
        cq0 = [r for r in col_rows if r[2] == 0]
        L.append(f'# 開幕の植民地（四半期0の行が Colonial・選ばれた国）：州×種 {len(cq0)}・q0 の支配度 中央 {med([r[3] for r in cq0]):.4f}・q0 の margin 中央 {med([r[4] for r in cq0]):.3f}・q0 で margin≤0 {sum(1 for r in cq0 if not (r[4] > 0))}')
        cy = [r for r in col_rows if r[2] == 3]
        L.append(f'# 開幕の植民地（年の末の行だけの国・最初の行 q3）：州×種 {len(cy)}・q3 の支配度 中央 {med([r[3] for r in cy]):.4f}')
        L.append('# 分離の建国（cederule_s の Seceded）：種\t全期間\t10年\t1年\tdominanceBefore=0\t10年で植民地格から\t29州の分離（州,四半期）')
        for c in sec:
            L.append('\t'.join(map(str, [c['s'], c['nsec'], c['nsec10'], c['nsec1'], c['nsec_d0'], c['col_sec10'], ';'.join(f'{r}@{q}' for r, q in c['tsec'])])))
        tot = [sum(c[k] for c in sec) for k in ['nsec', 'nsec10', 'nsec1', 'nsec_d0', 'col_sec10']]
        L.append('合計\t' + '\t'.join(map(str, tot)))
        summary.append((arm, tot))
        open(os.path.join(outd, f'paper946_{arm}.txt'), 'w', encoding='utf-8', newline='\n').write('\n'.join(L) + '\n')
    S = ['# 3 本の並べ：arm\t分離の建国 全期間\t10年\t1年\tdominanceBefore=0\t10年で植民地格から']
    for arm, tot in summary: S.append(arm + '\t' + '\t'.join(map(str, tot)))
    open(os.path.join(outd, 'paper946_cmp.txt'), 'w', encoding='utf-8', newline='\n').write('\n'.join(S) + '\n')

if __name__ == '__main__':
    main()
