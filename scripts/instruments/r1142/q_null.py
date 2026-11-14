# (1142) c111cr 設計の材料：段 C′ の 2 回目の門の閾値をヌル腕の生から出す（読むだけ・コミットしない）
# 呼び方：python -X utf8 -I q_null.py <出力フォルダ> base=<生> n1=<生> n2=<生> armB=<生>
# 読む表：rulepower_s（全ての国の全ての州が毎四半期）・rule_s（計器の名簿 9 国が毎四半期・他は年の末）
# 数える物（種ごと→種の中央）：
#   F：外国の取り分 nowRule2Share の州の数（>0・≧0.01・≧0.05・≧0.25）と尾（0<x<0.01）の数と質量（四半期の平均）
#   G1：交戦していない持ち主（wNationIndex＝0 かつ wEngageNationIndex＝0）の Core の州で x≧0.25 の数（種×四半期の最大）
#   TP：港（world.json・landmass.json の coastal）で x≧0.25 の州がある国×四半期の割合＝全ての港（今の TreatyPortInForce の組み直し）と
#       本国格の港だけ（tier≠Colonial＝tp の直し H の組み直し）。rule_s の treatyPortInForceFlag（エンジンの値）と同じ行で突き合わせる。
#   C8：L（lNationIndex）が前の 4 四半期の平均の 0.8 倍以下と 0.95〜1.05 倍の国の、平均 nowRule0Share の 4 四半期の変化の差（O＝0／O＞0 で分ける）。
#       O は oEngNationIndex が有限ならそれ・無ければ影の oNationIndex（旗 0 の生）。gates_rc.py:169〜183 と同じ定義。
import sys, os, json, math, statistics as st
from collections import defaultdict, Counter
outd = sys.argv[1]; arms = [a.split('=', 1) for a in sys.argv[2:]]
os.makedirs(outd, exist_ok=True)
ROOT = 'C:/Users/dirge/Documents/PriceOfBread/PriceOfBread.SimCore'
wd = json.load(open(ROOT + '/world.json', encoding='utf-8'))
port = {}
for n in wd['nations']:
    for p in n['provinces']: port.setdefault(p['city'], bool(p.get('coastal')))
for x in json.load(open(ROOT + '/landmass.json', encoding='utf-8')): port.setdefault(x['name'], bool(x.get('coastal')))
LINE = 0.25
FOCUS = ['イギリス', 'ロシア', 'フランス', '清', '日本', 'オスマン', 'アメリカ', 'オランダ', 'スペイン']
def fl(s):
    try: return float(s)
    except ValueError: return float('nan')
def fin(*v): return all(not (math.isnan(x) or math.isinf(x)) for x in v)
def med(xs): return st.median(xs) if xs else float('nan')
out = open(os.path.join(outd, 'q_null.txt'), 'w', encoding='utf-8', newline='\n')
def P(*a): print(*a, file=out)
summ = {}
for lb, ins in arms:
    per = defaultdict(list)          # 指標 -> 種ごとの値
    tp = defaultdict(lambda: [0, 0, 0])   # nation -> [行, 全ての港の旗, 本国の港の旗]
    chk = Counter()                  # rule_s との突き合わせ
    for seed in range(1, 11):
        p = os.path.join(ins, f'rulepower_s{seed}.tsv')
        if not os.path.exists(p): continue
        qc = defaultdict(lambda: [0, 0, 0, 0, 0, 0.0])   # q -> [>0, ≧.01, ≧.05, ≧.25, 尾の数, 尾の質量]
        g1 = Counter(); natq = defaultdict(dict); flags = {}
        with open(p, encoding='utf-8') as f:
            h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
            iq, inn, ir, it = ix['quarter'], ix['nation'], ix['region'], ix['tier']
            i0, i2, iw, ie, iL = ix['nowRule0Share'], ix['nowRule2Share'], ix['wNationIndex'], ix['wEngageNationIndex'], ix['lNationIndex']
            iO, iOe = ix['oNationIndex'], ix.get('oEngNationIndex')
            for ln in f:
                c = ln.rstrip('\n').split('\t')
                q = int(c[iq]); nat = c[inn]; x = fl(c[i2])
                a = qc[q]
                if x > 0:
                    a[0] += 1
                    if x >= 0.01: a[1] += 1
                    if x >= 0.05: a[2] += 1
                    if x >= 0.25: a[3] += 1
                    if x < 0.01: a[4] += 1; a[5] += x
                if x >= LINE and c[it] == 'Core' and fl(c[iw]) == 0 and fl(c[ie]) == 0: g1[q] += 1
                k = (q, nat)
                fa, fh = flags.get(k, (False, False))
                if port.get(c[ir], False) and x >= LINE:
                    fa = True
                    if c[it] != 'Colonial': fh = True
                flags[k] = (fa, fh)
                L = fl(c[iL]); O = fl(c[iOe]) if iOe is not None else float('nan')
                if not fin(O): O = fl(c[iO])
                e = natq[q].get(nat)
                if e is None: e = natq[q][nat] = [L, O, 0.0, 0]
                n0 = fl(c[i0])
                if fin(n0): e[2] += n0; e[3] += 1
        qs = sorted(qc)
        for j, name in enumerate(['gt0', 'ge01', 'ge05', 'ge25', 'tailN', 'tailMass']):
            per[name].append(sum(qc[q][j] for q in qs) / len(qs))
        per['G1max'].append(max(g1.values()) if g1 else 0)
        for (q, nat), (fa, fh) in flags.items():
            t = tp[nat]; t[0] += 1; t[1] += fa; t[2] += fh
            t2 = tp['＊全て']; t2[0] += 1; t2[1] += fa; t2[2] += fh
        # rule_s のエンジンの値と組み直し（全ての港）の一致
        pr = os.path.join(ins, f'rule_s{seed}.tsv')
        if os.path.exists(pr):
            with open(pr, encoding='utf-8') as f:
                h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
                iq, inn, itf = ix['quarter'], ix['nation'], ix['treatyPortInForceFlag']
                seen = set()
                for ln in f:
                    c = ln.rstrip('\n').split('\t')
                    k = (int(c[iq]), c[inn])
                    if k in seen or k not in flags: continue
                    seen.add(k)
                    eng = fl(c[itf]) > 0
                    rec = flags[k][0]
                    chk['rows'] += 1; chk['match'] += (eng == rec)
                    if eng and not rec: chk['engOnly'] += 1
                    if rec and not eng: chk['recOnly'] += 1
        # C8（gates_rc.py:169〜183 と同じ）
        d0, s0, d1, s1 = [], [], [], []
        for qi, q in enumerate(qs):
            if qi < 4: continue
            for nat, v in natq[q].items():
                back = [natq[qs[qi - j]].get(nat) for j in range(1, 5)]
                if any(b is None for b in back) or v[3] == 0 or back[3][3] == 0: continue
                mL = sum(b[0] for b in back) / 4
                if not (mL > 0 and fin(v[0], v[1])): continue
                r = v[0] / mL; dt = v[2] / v[3] - back[3][2] / back[3][3]
                grp = (d0, s0) if v[1] == 0 else (d1, s1)
                if r <= 0.8: grp[0].append(dt)
                elif 0.95 <= r <= 1.05: grp[1].append(dt)
        if d0 and s0: per['C8c'].append(sum(d0) / len(d0) - sum(s0) / len(s0))
        if d1 and s1: per['C8d'].append(sum(d1) / len(d1) - sum(s1) / len(s1))
        per['C8c_nDrop'].append(len(d0)); per['C8d_nDrop'].append(len(d1))
    summ[lb] = {k: med(v) for k, v in per.items()}
    P(f'## {lb}  {ins}  種 {len(per["gt0"])}')
    for k, v in per.items(): P(f'  {k}\t中央 {med(v):.6g}\t種ごと ' + ' '.join(f'{x:.4g}' for x in v))
    P(f'  G1 種ごとの最大の和 {sum(per["G1max"])}')
    P(f'  TP rule_s との突き合わせ（全ての港の組み直し vs treatyPortInForceFlag）：行 {chk["rows"]}・一致 {chk["match"]}・エンジンだけ {chk["engOnly"]}・組み直しだけ {chk["recOnly"]}')
    for nat in FOCUS + ['＊全て']:
        t = tp.get(nat)
        if not t or t[0] == 0: continue
        P(f'  TP {nat}\t国×四半期 {t[0]}\t全ての港 {t[1] / t[0]:.4f}\t本国の港 {t[2] / t[0]:.4f}')
P('')
P('## ヌル腕の幅（base・n1・n2）と 1＋2×(幅−1) の上側（幅＝最大÷最小・正の量だけ）')
for k in ['gt0', 'ge01', 'ge05', 'ge25', 'tailN', 'tailMass', 'G1max']:
    vs = [summ[a][k] for a in ('base', 'n1', 'n2') if a in summ]
    if len(vs) == 3 and min(vs) > 0:
        w = max(vs) / min(vs); P(f'  {k}\t' + ' '.join(f'{v:.4g}' for v in vs) + f'\t幅 {w:.4f}\t上側の倍率 {1 + 2 * (w - 1):.4f}\t上側 {max(vs) * (1 + 2 * (w - 1)):.4g}' + (f'\t腕B（C′1 回目）{summ["armB"][k]:.4g}' if 'armB' in summ else ''))
    else: P(f'  {k}\t' + ' '.join(f'{v:.4g}' for v in vs) + '\t幅は出せない（0 を含む）')
for k in ['C8c', 'C8d']:
    vs = [summ[a][k] for a in ('base', 'n1', 'n2') if a in summ]
    P(f'  {k}\t' + ' '.join(f'{v:.4g}' for v in vs) + f'\t最小−幅 {min(vs) - (max(vs) - min(vs)):.4g}\t最大＋幅 {max(vs) + (max(vs) - min(vs)):.4g}' + (f'\t腕B {summ["armB"][k]:.4g}' if 'armB' in summ else ''))
out.close()
