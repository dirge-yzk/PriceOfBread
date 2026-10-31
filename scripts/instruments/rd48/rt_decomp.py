#!/usr/bin/env python
"""(710) rd48 ①：往復の総量の分解（読むだけ・C# は変えない・既存の列 urxrt_s だけで読む）。
使い方：python -X utf8 -I rt_decomp.py <計器の出力 ins フォルダ>[:<種の起点>] [--out <TSV の出力フォルダ>]
定義（国×十年ごと・種の平均・千人）：四半期 q・路 r（PopDecision・Meritocracy・Military・MultiNationSim）の入り in_rq と出 out_rq（都市への入り・都市からの出）。
  総量 G＝Σ_q Σ_r (in_rq＋out_rq)
  W＝路の中・四半期の中の打ち消し＝Σ_q Σ_r 2·min(in_rq, out_rq)（同じ路が同じ四半期に両向きに動いた量＝州どうしか月どうしの逆向き）
  C＝路どうし・四半期の中の打ち消し＝Σ_q (Σ_r |net_rq| − |Σ_r net_rq|)（例：PopDecision の入りを同じ四半期の社会移動の出が打ち消す）
  A＝四半期をまたぐ打ち消し＝Σ_q |net_q| − |Σ_q net_q|（四半期ごとの純の向きが十年の中で替わる）
  N＝十年の純の絶対値＝|Σ_q net_q|
  恒等式 G＝W＋C＋A＋N（構成上・桁落ちだけ）。戻す割合 ret＝(G−N)/G（urbd 段4 の〈往復の戻す割合〉の候補の物差し）。
  PopDecision だけの戻す割合 retPD＝1−|Σ_q netPD_q|／Σ_q(inPD_q＋outPD_q)。路ごとの WPD・APD（四半期の中／四半期をまたぐ）も出す。
限界の申告：urxrt_s は国×四半期×路の和＝四半期の中の〈州どうし〉と〈月どうし〉は分けられない（W に両方入る）。挙げた国の POP だけ・助走は数えない（InstrumentsUrx.cs の頭）。
  路 Military は兵士の席への出入り（Meritocracy の志願・脱走も Military.MoveCohort を通るのでここに入る）。疫病・出生・死は Blend を通らないので入らない。
"""
import csv, glob, os, re, sys
from collections import defaultdict

args = sys.argv[1:]
outdir = None
if '--out' in args:
    i = args.index('--out'); outdir = args[i + 1]; del args[i:i + 2]
D = args[0]; s0 = 1
m = re.match(r'^(.*):(\d+)$', D)
if m and not re.match(r'^[A-Za-z]$', m.group(1)): D, s0 = m.group(1), int(m.group(2))
seeds = list(range(s0, s0 + 10))
q = defaultdict(lambda: [0.0, 0.0])   # (seed, nation, quarter, route) -> in, out
yr = {}
for s in seeds:
    p = os.path.join(D, f'urxrt_s{s}.tsv')
    with open(p, encoding='utf-8', newline='') as f:
        rd = csv.reader(f, delimiter='\t'); h = next(rd); ix = {k: i for i, k in enumerate(h)}
        for x in rd:
            if len(x) != len(h): continue
            k = (s, x[ix['nation']], int(x[ix['quarter']]), x[ix['route']])
            a = q[k]; a[0] += float(x[ix['urbanInK']]); a[1] += float(x[ix['urbanOutK']])
            yr[int(x[ix['quarter']])] = int(x[ix['year']])
routes = sorted({k[3] for k in q})
# 種×国×十年へ
agg = defaultdict(lambda: defaultdict(float))
byq = defaultdict(lambda: defaultdict(lambda: [0.0, 0.0]))   # (seed,nat,dec) -> quarter -> per route handled below
perq = defaultdict(dict)   # (seed,nat,quarter) -> route -> (in,out)
for (s, nat, qq, r), (i, o) in q.items(): perq[(s, nat, qq)][r] = (i, o)
dec_q = defaultdict(list)
for (s, nat, qq) in perq: dec_q[(s, nat, yr[qq] // 10 * 10)].append(qq)
rows = {}
for (s, nat, dec), qs in dec_q.items():
    G = W = C = A = 0.0; netsum = 0.0; absq = 0.0
    gPD = netPD = absPD = wPD = 0.0; gM = netM = 0.0
    for qq in qs:
        rr = perq[(s, nat, qq)]
        netq = 0.0; sumabs = 0.0
        for r, (i, o) in rr.items():
            G += i + o; W += 2 * min(i, o); n = i - o; netq += n; sumabs += abs(n)
            if r == 'PopDecision': gPD += i + o; netPD += n; absPD += abs(n); wPD += 2 * min(i, o)
            if r == 'Meritocracy': gM += i + o; netM += n
        C += sumabs - abs(netq); absq += abs(netq); netsum += netq
    N = abs(netsum); A = absq - N
    rows[(s, nat, dec)] = dict(G=G, W=W, C=C, A=A, N=N, gPD=gPD, netPD=netPD, wPD=wPD, aPD=absPD - abs(netPD), gM=gM, netM=netM)
nats = sorted({k[1] for k in rows}); decs = sorted({k[2] for k in rows})
keys = ['G', 'W', 'C', 'A', 'N', 'gPD', 'netPD', 'wPD', 'aPD', 'gM', 'netM']
def mean(nat, dec, k):
    v = [rows[(s, nat, dec)][k] for s in seeds if (s, nat, dec) in rows]
    return sum(v) / len(v) if v else float('nan'), len(v)
hdr = 'nation\tdecade\tseeds\tgrossK\tW_inQ_inRoute\tC_inQ_crossRoute\tA_acrossQ\tN_net\tret\tW/G\tC/G\tA/G\tPD_grossK\tPD_netK\tPD_ret\tPD_W/G\tPD_A/G\tMer_grossK\tMer_netK\tMer/PD_net'
lines = [hdr]
tot = defaultdict(float)
for nat in nats:
    for dec in decs:
        vals = {k: mean(nat, dec, k)[0] for k in keys}; ns = mean(nat, dec, 'G')[1]
        if ns == 0: continue
        G = vals['G']
        if G <= 0: continue
        ret = (G - vals['N']) / G
        pdret = 1 - abs(vals['netPD']) / vals['gPD'] if vals['gPD'] > 0 else float('nan')
        ratio = vals['netM'] / vals['netPD'] if abs(vals['netPD']) > 1e-9 else float('nan')
        lines.append(f"{nat}\t{dec}\t{ns}\t{G:.1f}\t{vals['W']:.1f}\t{vals['C']:.1f}\t{vals['A']:.1f}\t{vals['N']:.1f}\t{ret:.3f}\t{vals['W']/G:.3f}\t{vals['C']/G:.3f}\t{vals['A']/G:.3f}\t{vals['gPD']:.1f}\t{vals['netPD']:.1f}\t{pdret:.3f}\t"
                     + (f"{vals['wPD']/vals['gPD']:.3f}\t{vals['aPD']/vals['gPD']:.3f}" if vals['gPD'] > 0 else 'nan\tnan') + f"\t{vals['gM']:.1f}\t{vals['netM']:.1f}\t{ratio:.2f}")
        for k in keys: tot[(nat, k)] += vals[k]
        for k in keys: tot[('ALL', k)] += vals[k]
        tot[(nat, 'absNetPD')] += abs(vals['netPD']); tot[('ALL', 'absNetPD')] += abs(vals['netPD'])
        for kk in (nat, 'ALL'): tot[(kk, 'sumNetPD')] += vals['netPD']; tot[(kk, 'sumNetM')] += vals['netM']
lines.append('')
lines.append('# 国ごとの全期間（十年の値の和＝十年の純を足しても全期間の純にはならない＝A は十年の中だけ）')
lines.append('nation\tgrossK\tW/G\tC/G\tA/G\tN/G\tret\tPD_grossK\tPD_W/G\tPD_A/G\tPD_ret')
for nat in nats + ['ALL']:
    G = tot[(nat, 'G')]
    if G <= 0: continue
    gp = tot[(nat, 'gPD')]
    lines.append(f"{nat}\t{G:.0f}\t{tot[(nat,'W')]/G:.3f}\t{tot[(nat,'C')]/G:.3f}\t{tot[(nat,'A')]/G:.3f}\t{tot[(nat,'N')]/G:.3f}\t{(G-tot[(nat,'N')])/G:.3f}\t{gp:.0f}\t{tot[(nat,'wPD')]/gp:.3f}\t{tot[(nat,'aPD')]/gp:.3f}\t{1-tot[(nat,'absNetPD')]/gp:.3f}")
lines.append('')
lines.append('# urbd 段4 の物差し（設計書の段4 の門の案）＝社会移動の都市への純÷PopDecision の都市への純の符号を返した比＝−ΣnetMer÷ΣnetPD（全期間・種の平均・国ごと）')
lines.append('nation\tPD_netK\tMer_netK\tminusMer/PD')
for nat in nats + ['ALL']:
    pdn = tot[(nat, 'sumNetPD')]; mn = tot[(nat, 'sumNetM')]
    lines.append(f"{nat}\t{pdn:.0f}\t{mn:.0f}\t" + (f"{-mn/pdn:.3f}" if abs(pdn) > 1e-9 else 'nan'))
txt = '\n'.join(lines) + '\n'
# 恒等式の残差
res = max(abs(r['G'] - (r['W'] + r['C'] + r['A'] + r['N'])) / max(r['G'], 1e-9) for r in rows.values())
txt += f'# 恒等式 G＝W＋C＋A＋N の相対残差の最大＝{res:.2e}（種×国×十年 {len(rows)} 件）・種={seeds}・路={routes}\n'
if outdir:
    os.makedirs(outdir, exist_ok=True)
    with open(os.path.join(outdir, 'rt_decomp.tsv'), 'wb') as f: f.write(txt.encode('utf-8'))
sys.stdout.write(txt)
