# -*- coding: utf-8 -*-
"""(708) prd2 影の N の読み手（読むだけ・シミュを回さない）。
prds_s<種>.tsv の末尾 10 列（nBodyComShadow ほか・InstrumentsPrds.cs）と prdf_s<種>.tsv を読み、
  1. 計器の自己点検（今の主軸の列 …Real と、Factions.Step が出す prdf_s の nBodyCom・nBodySoc ほか）
  2. 影の N の十年ごとの表と閾値の感度
  3. 国ごとの表・イギリスの割合（設計書 §3-2 の〈影の N が発見した行の何 % で、イギリスが半分以上か〉）
  4. 政体ごと・組合法ごと
  5. 持続（同じ最大のセルで N が 1 以上の連続）
  6. 頻度の物差し（推）：先進国の何か国が政体 6（プロレタリア独裁）へ届く見込みか
を 1 枚の表にする。影は旗 0 の世界の上の数＝旗 0 の世界は基線と全ビット同じ。
使い方：python shadow_n.py <計器の出力フォルダ> [種の数=10] [基線の名乗り] [種の起点=1]
"""
import sys, os, csv, collections, math, statistics

root = sys.argv[1]
nseed = int(sys.argv[2]) if len(sys.argv) > 2 else 10
tag = sys.argv[3] if len(sys.argv) > 3 else ''
seed0 = int(sys.argv[4]) if len(sys.argv) > 4 else 1

POLITY = {0: '絶対王政', 1: '専制', 2: '幕府', 3: '立憲君主制', 4: '共和制', 5: 'ブルジョワ独裁', 6: 'プロ独裁', 7: '軍事独裁', 8: '無政府'}
LAW = {0: '非合法', 1: '統制', 2: '容認'}
# 先進国の定義（推）：計器の 9 国（清・ロシア・オスマン・フランス・イギリス・プロイセン・オーストリア・日本・アメリカ）のうち、
# 1900 年前後の工業国の自称〈欧米日〉＝核は英仏普米、広は核にオーストリア・日本を足した物（ロシア・清・オスマンは含めない）。
ADV_CORE = ['イギリス', 'フランス', 'プロイセン', 'アメリカ']
ADV_WIDE = ADV_CORE + ['オーストリア', '日本']
REPUBLIC, PD = 4, 6


def num(v):
    try: return float(v)
    except ValueError: return float('nan')


def read(path):
    with open(path, 'r', encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t')
        h = next(r)
        ix = {k: i for i, k in enumerate(h)}
        out = []
        for x in r:
            if len(x) != len(h): continue
            out.append(x)
    return ix, out


def med(v): return statistics.median(v) if v else float('nan')
def mean(v): return sum(v) / len(v) if v else float('nan')
def pct(a, b): return (100.0 * a / b) if b else float('nan')
def quant(v, p):
    v = sorted(v)
    return v[min(len(v) - 1, max(0, int(round(p * (len(v) - 1)))))] if v else float('nan')


# ---------- 読み込み ----------
S = {}      # (seed, nation) -> list of rows(dict) 四半期順
F = {}      # (seed, quarter, nation) -> prdf 行(dict)
seeds = []
missing_cols = None
for s in range(seed0, seed0 + nseed):
    p = os.path.join(root, f'prds_s{s}.tsv')
    if not os.path.exists(p): continue
    ix, rows = read(p)
    need = ['nBodyComShadow', 'nCellComShadow', 'maxCellFracComShadow', 'nBodyComReal', 'nCellComReal', 'maxCellFracComReal', 'nBodySocShadow', 'nBodySocReal', 'topRegionComShadow', 'topBlocComShadow']
    if any(k not in ix for k in need):
        missing_cols = [k for k in need if k not in ix]
        break
    seeds.append(s)
    for x in rows:
        d = {k: x[i] for k, i in ix.items()}
        d['q'] = int(num(d['quarter'])); d['y'] = int(num(d['year'])); d['pol'] = int(num(d['polityCode'])); d['law'] = int(num(d['unionLaw']))
        d['era'] = num(d['eraComm'])
        for k in ('nBodyComShadow', 'nCellComShadow', 'nBodyComReal', 'nCellComReal', 'nBodySocShadow', 'nBodySocReal', 'topRegionComShadow', 'topBlocComShadow'):
            d[k] = int(num(d[k]))
        for k in ('maxCellFracComShadow', 'maxCellFracComReal', 'popK'):
            d[k] = num(d[k])
        S.setdefault((s, d['nation']), []).append(d)
    pf = os.path.join(root, f'prdf_s{s}.tsv')
    if os.path.exists(pf):
        ix2, rows2 = read(pf)
        for x in rows2:
            d = {k: x[i] for k, i in ix2.items()}
            F[(s, int(num(d['quarter'])), d['nation'])] = d
if missing_cols:
    print('列が無い（新しい計器の出力でない）：', missing_cols); sys.exit(1)
for k in S: S[k].sort(key=lambda d: d['q'])

allrows = [d for L in S.values() for d in L]
disc = [d for d in allrows if d['era'] == 1.0]
out = []
P = out.append
P(f'# prd2 (708) 影の N の表（読み手 scripts/instruments/prd2/shadow_n.py・読むだけ）{(" 基線＝" + tag) if tag else ""}')
P(f'# 入力＝{root}　種 {len(seeds)}（{seeds[0]}〜{seeds[-1]}）　行（国×四半期）{len(allrows)}　共産主義を見つけた行（eraComm＝1）{len(disc)}')
P('# 影の N ＝ 影の主軸が共産主義のセル（地域×束）のうち、規模（参加の重みつきの人数）が全国人口の 1% 以上の数。設計書の nCellComShadow1pct に当たる（足切りは FactionSnapshot.MinSizeFraction）。')
P('# 影は旗 0 の世界の上の数（移し替えの帰結＝闘争度の応答・結束・蜂起の門・政体の遷移は入らない）。分母の注意：行は全 61 カ国（建国の国を含む）の国×四半期。')
P(f'# 先進国（推の定義）：核＝{"・".join(ADV_CORE)}／広＝核＋{"・".join(ADV_WIDE[len(ADV_CORE):])}（ロシア・清・オスマンは含めない）。')
P('')

# ---------- 1. 自己点検 ----------
P('## 1. 計器の自己点検（今の主軸の列 …Real と prdf_s の同じ量・同じ四半期の同じ国）')
join = [(d, F[(s, d['q'], d['nation'])]) for (s, n), L in S.items() for d in L if (s, d['q'], d['nation']) in F]
noF = len(allrows) - len(join)
chk = [('nBodyComReal', 'nBodyCom', 'int'), ('nBodySocReal', 'nBodySoc', 'int'), ('nCellComReal', 'nCellCom', 'int'), ('maxCellFracComReal', 'maxCellFracCom', 'f')]
P(f'結合できた行 {len(join)}（prdf_s に行が無い prds_s の行 {noF}＝議会の無い世界など）')
P('| 量 | 一致の行 | 不一致の行 | 不一致の割合 % | 不一致の最大の差 | 基線（prdf）が 0 でない行 |')
P('|---|---|---|---|---|---|')
mism_rows = collections.defaultdict(list)
for a, b, kind in chk:
    ok = bad = nz = 0; mx = 0.0
    for d, f in join:
        va = d[a]; vb = num(f[b])
        if vb != 0: nz += 1
        diff = abs(va - vb)
        if (kind == 'int' and diff > 0) or (kind == 'f' and diff > 1e-9):
            bad += 1; mx = max(mx, diff); mism_rows[a].append((d, f))
        else: ok += 1
    P(f'| {a} ↔ {b} | {ok} | {bad} | {pct(bad, ok + bad):.3f} | {mx:.4g} | {nz} |')
P('')
sm = mism_rows['nBodySocReal']
if sm:
    P(f'nBodySocReal の不一致 {len(sm)} 行の内訳（prds が多い／少ない・政体・年の十年）：')
    cnt = collections.Counter(('prdsが多い' if d['nBodySocReal'] > num(f['nBodySoc']) else 'prdsが少ない') for d, f in sm)
    P('  ' + '　'.join(f'{k} {v}' for k, v in cnt.items()))
    dec = collections.Counter(d['y'] // 10 * 10 for d, f in sm)
    P('  十年：' + '　'.join(f'{k}s {v}' for k, v in sorted(dec.items())))
    P('  例（種・年・国・prds の値・prdf の値）：' + '；'.join(f'{d["q"]}Q {d["nation"]} {d["nBodySocReal"]}/{f["nBodySoc"]}' for d, f in sm[:6]))
P('限界：prds_s は思想の更新（UpdateShares）の時点、prdf_s は Factions.Step の終わりの断面＝同じ四半期でも闘争度・生活の参照点・人口の更新の差が出る。不一致の割合がその時点差の大きさ。')
P('')

# ---------- 2. 十年ごと ----------
P('## 2. 十年ごと（共産主義を見つけた国×四半期）')
P('| 十年 | 行 | N≧1 の行 | N≧1 の割合 % | N≧2 の行 | 最大のセルの割合 平均 | P90 | 最大 | ≧0.5% の行 | ≧1%（=N≧1） | ≧2% の行 | 影の共産主義主軸の割合 平均 |')
P('|---|---|---|---|---|---|---|---|---|---|---|---|')
bydec = collections.defaultdict(list)
for d in disc: bydec[d['y'] // 10 * 10].append(d)
for dd in sorted(bydec):
    L = bydec[dd]; m = [d['maxCellFracComShadow'] for d in L]
    P(f'| {dd}s | {len(L)} | {sum(1 for d in L if d["nBodyComShadow"] >= 1)} | {pct(sum(1 for d in L if d["nBodyComShadow"] >= 1), len(L)):.2f} | {sum(1 for d in L if d["nBodyComShadow"] >= 2)} | {mean(m):.5f} | {quant(m, .9):.5f} | {max(m):.4f} | {sum(1 for v in m if v >= 0.005)} | {sum(1 for v in m if v >= 0.01)} | {sum(1 for v in m if v >= 0.02)} | {mean([num(d["shadowComAxisShare"]) for d in L]):.4f} |')
nN = sum(1 for d in disc if d['nBodyComShadow'] >= 1)
P(f'全体：N≧1 の行 {nN}／{len(disc)}（{pct(nN, len(disc)):.2f}%）。今の主軸（移し替えなし）で N≧1 の行 {sum(1 for d in disc if d["nBodyComReal"] >= 1)}（基線の共産主義の運動体）。')
P('')

# ---------- 3. 国ごと ----------
P('## 3. 国ごと（N≧1 の行の多い順の上位 25・全ての十年・種をまたいだ行）')
bynat = collections.defaultdict(list)
for d in disc: bynat[d['nation']].append(d)
rank = sorted(bynat.items(), key=lambda kv: -sum(1 for d in kv[1] if d['nBodyComShadow'] >= 1))
P('| 国 | 行 | N≧1 の行 | 割合 % | 行にいる種 | N≧1 の種 | 最大のセルの割合 最大 | 最初の N≧1 の年（種の中央） |')
P('|---|---|---|---|---|---|---|---|')
for nat, L in rank[:25]:
    nz = [d for d in L if d['nBodyComShadow'] >= 1]
    sd = {s for (s, n) in S if n == nat}
    first = []
    for s in seeds:
        z = [d['y'] for d in S.get((s, nat), []) if d['era'] == 1.0 and d['nBodyComShadow'] >= 1]
        if z: first.append(min(z))
    nzs = len({s for s in seeds if any(x['nBodyComShadow'] >= 1 and x['era'] == 1.0 for x in S.get((s, nat), []))})
    P(f'| {nat} | {len(L)} | {len(nz)} | {pct(len(nz), len(L)):.2f} | {len(sd)} | {nzs} | {max(d["maxCellFracComShadow"] for d in L):.4f} | {med(first):.0f} |')
P('')
P('### 3-1. 設計書 §3-2 の線：影の N が発見した行の何 % で、イギリスが半分以上か')
uk = [d for d in disc if d['nation'] == 'イギリス']
ukN = sum(1 for d in uk if d['nBodyComShadow'] >= 1)
P(f'- 発見した行 {len(disc)} のうち N≧1 の行 {nN}（{pct(nN, len(disc)):.2f}%）。うちイギリスの行 {ukN}（N≧1 の行の {pct(ukN, nN):.1f}%・イギリス自身の発見した行 {len(uk)} の {pct(ukN, len(uk)):.1f}%）。')
P('| 十年 | N≧1 の行 | うちイギリス | イギリスの割合 % | 先進国（広）の割合 % |')
P('|---|---|---|---|---|')
for dd in sorted(bydec):
    L = bydec[dd]; z = [d for d in L if d['nBodyComShadow'] >= 1]
    u = sum(1 for d in z if d['nation'] == 'イギリス'); a = sum(1 for d in z if d['nation'] in ADV_WIDE)
    P(f'| {dd}s | {len(z)} | {u} | {pct(u, len(z)):.1f} | {pct(a, len(z)):.1f} |')
half = 0; per = []
for s in seeds:
    z = [d for (ss, n), L in S.items() if ss == s for d in L if d['era'] == 1.0 and d['nBodyComShadow'] >= 1]
    u = sum(1 for d in z if d['nation'] == 'イギリス')
    per.append((s, len(z), u))
    if z and 2 * u >= len(z): half += 1
P('- 種ごと（N≧1 の行・うちイギリス）：' + '　'.join(f'種{s}:{n}/{u}' for s, n, u in per))
P(f'- イギリスが N≧1 の行の半分以上の種：{half}/{len(seeds)}。（設計書の線〈イギリスが半分以上〉は、この種の数と上の全体の割合で読む。割合の閾値〈数 %〉は作者が決めていない。）')
P('')

# ---------- 4. 政体・組合法 ----------
P('## 4. 政体ごと・組合法ごと（共産主義を見つけた行）')
P('| 政体 | 行 | N≧1 の行 | 割合 % |')
P('|---|---|---|---|')
bp = collections.defaultdict(list)
for d in disc: bp[d['pol']].append(d)
for k in sorted(bp):
    L = bp[k]; z = sum(1 for d in L if d['nBodyComShadow'] >= 1)
    P(f'| {POLITY.get(k, k)} | {len(L)} | {z} | {pct(z, len(L)):.2f} |')
P('')
P('| 組合法 | 行 | N≧1 の行 | 割合 % |')
P('|---|---|---|---|')
bl = collections.defaultdict(list)
for d in disc: bl[d['law']].append(d)
for k in sorted(bl):
    L = bl[k]; z = sum(1 for d in L if d['nBodyComShadow'] >= 1)
    P(f'| {LAW.get(k, k)} | {len(L)} | {z} | {pct(z, len(L)):.2f} |')
P('')

# ---------- 5. 持続 ----------
P('## 5. 持続（N≧1 が続く連続の長さ）')
P('結束は同じ州×束×主軸のセルに育つ（Factions.Step）＝連続は〈N≧1 かつ最大のセルが前の四半期と同じ（topRegion・topBloc）〉で数える。国全体の連続（セルを問わず N≧1）は上限。')
runs_cell = []   # (seed, nation, length, startQ)
runs_nat = []
for (s, nat), L in S.items():
    cur_c = cur_n = 0; prev = None; start = None
    for d in L:
        on = d['era'] == 1.0 and d['nBodyComShadow'] >= 1
        if on:
            key = (d['topRegionComShadow'], d['topBlocComShadow'])
            cur_n += 1
            if prev is not None and key == prev: cur_c += 1
            else:
                if cur_c > 0: runs_cell.append((s, nat, cur_c, start))
                cur_c = 1; start = d['q']
            prev = key
        else:
            if cur_c > 0: runs_cell.append((s, nat, cur_c, start))
            if cur_n > 0: runs_nat.append((s, nat, cur_n))
            cur_c = cur_n = 0; prev = None
    if cur_c > 0: runs_cell.append((s, nat, cur_c, start))
    if cur_n > 0: runs_nat.append((s, nat, cur_n))
P(f'連続（同じセル）{len(runs_cell)} 本・中央値 {med([r[2] for r in runs_cell]):.0f} 四半期／連続（国全体）{len(runs_nat)} 本・中央値 {med([r[2] for r in runs_nat]):.0f} 四半期。')
P('| L（四半期） | 同じセルで L 以上続く連続 | うち先進国（広） | 国×種の数 | うち先進国（広）の国×種 | うち先進国（核）の国×種 | 国全体で L 以上の連続 |')
P('|---|---|---|---|---|---|---|')
Ls = [4, 8, 16, 24, 30, 40, 60]
for Lm in Ls:
    rc = [r for r in runs_cell if r[2] >= Lm]
    ns = {(r[0], r[1]) for r in rc}
    P(f'| {Lm} | {len(rc)} | {sum(1 for r in rc if r[1] in ADV_WIDE)} | {len(ns)} | {sum(1 for x in ns if x[1] in ADV_WIDE)} | {sum(1 for x in ns if x[1] in ADV_CORE)} | {sum(1 for r in runs_nat if r[2] >= Lm)} |')
P('')

# ---------- 6. 頻度の物差し（推） ----------
P('## 6. 頻度の物差し（推・確信 低）：影の式のまま点火したら、1 試合に先進国の何か国が政体 6（プロ独裁）へ届く見込みか')
P('鎖：入口（共産主義のセルが N≧1 で L 四半期続く＝結束が内乱化の門 FactionCohesionFightThreshold 0.8 に届く）→ 点火の 2 門と法の経路 → 蜂起 → 反乱の勝ち（政体が目標へ）。')
P('結束の成長は 1 四半期 0.05×(0.5+束の意識)×(1−結束)（Factions.Step）＝0.8 に届く四半期の数は 束の意識 0.3/0.55/0.8 で 40/30/24。L はこの 3 通り。')
P('各段の変換率は旗 0 の基線の蜂起（prdf_s の revoltN・revoltTarget）から取る＝影の式が社会主義の束に当たる変換と同じ率で共産主義の束にも当たると仮定（推）。')
# 基線: 社会主義の連続（国全体・セルの同一性は prdf に無い）と蜂起
soc_runs = []   # (seed, nation, startIdx, endIdx)
rev_events = []
winN = polN = 0
FS = collections.defaultdict(list)
for (s, q, nat), f in F.items(): FS[(s, nat)].append((q, f))
for k in FS: FS[k].sort(key=lambda t: t[0])
for (s, nat), L in FS.items():
    cur = 0; st = None
    for i, (q, f) in enumerate(L):
        if num(f['nBodySoc']) >= 1:
            if cur == 0: st = i
            cur += 1
        else:
            if cur > 0: soc_runs.append((s, nat, st, i - 1))
            cur = 0
        if int(num(f['revoltN'])) > 0:
            t = int(num(f['revoltTarget'])); pc = int(num(f['polityCode']))
            rev_events.append((s, nat, i, t, pc))
            if t != pc:
                polN += 1
                if any(int(num(L[j][1]['polityCode'])) == t for j in range(i + 1, min(len(L), i + 17))): winN += 1
    if cur > 0: soc_runs.append((s, nat, st, len(L) - 1))
nrev = len(rev_events)
P(f'基線の蜂起 {nrev} 件（政体を変える目標の蜂起 {polN} 件・うち 16 四半期以内に政体が目標になった {winN} 件＝反乱の勝ち {pct(winN, polN):.0f}%）。社会主義の運動体の連続（国全体）{len(soc_runs)} 本。')
win = (winN / polN) if polN else float('nan')
P('| L | 基線：社会主義の連続が L 以上 | その連続の最中か直後に蜂起 | 連続あたりの蜂起 c_L | 影の共産主義：L 以上の連続（同じセル）の国×種（先進国・広） | 同（核） | 期待の蜂起（広・1 試合あたり） | 期待の政体 6 到達（広・1 試合あたりの国の数 ＝Σ(1−exp(−E))÷種） | 同（核） | 入口の上限：L 以上の連続を持つ先進国（広）の国×種÷種 |')
P('|---|---|---|---|---|---|---|---|---|---|')
for Lm in (24, 30, 40):
    sr = [r for r in soc_runs if r[3] - r[2] + 1 >= Lm]
    srset = collections.defaultdict(list)
    for r in sr: srset[(r[0], r[1])].append(r)
    nrv = 0
    for (s, nat, i, t, pc) in rev_events:
        if any(r[2] <= i <= r[3] + 4 for r in srset.get((s, nat), [])): nrv += 1
    c = (nrv / len(sr)) if sr else float('nan')
    # 影の共産主義の連続（広・核）
    perns_w = collections.Counter(); perns_c = collections.Counter()
    for r in runs_cell:
        if r[2] >= Lm:
            if r[1] in ADV_WIDE: perns_w[(r[0], r[1])] += 1
            if r[1] in ADV_CORE: perns_c[(r[0], r[1])] += 1
    Ew = sum(perns_w.values()) * c * win / len(seeds) if sr else float('nan')
    reach_w = sum(1 - math.exp(-n * c * win) for n in perns_w.values()) / len(seeds) if sr else float('nan')
    reach_c = sum(1 - math.exp(-n * c * win) for n in perns_c.values()) / len(seeds) if sr else float('nan')
    P(f'| {Lm} | {len(sr)} | {nrv} | {c:.3f} | {len(perns_w)} | {len(perns_c)} | {Ew:.2f} | {reach_w:.2f} | {reach_c:.2f} | {len(perns_w) / len(seeds):.2f} |')
P('')
P('読み方（推）：c_L は〈社会主義の運動体が L 四半期以上続いた間か直後の蜂起の件数÷その連続の数〉。蜂起は他の思想の運動体（自由主義・共和主義）からも起きるので、c_L は上限側の見積り（共産主義の運動体に当てはめると過大）。')
P('入口の上限は、共産主義のセルが L 以上続く先進国の数（点火の 2 門と蜂起を問わない）。期待の政体 6 到達は c_L×反乱の勝ち率を掛けた期待で、帰結（闘争度の応答・理想点の変化・束の結束の他の道）を含まない。')
P('作者の目安（2026-10-31 原文）〈1試合に先進国に1~2か国がそうなる、ぐらいが適切に見える。ファシストよりは少ないが、出ない程度という認識〉は記録の物差し（門にしない）。ファシズムの国の数は別（idn_s の記録）。')
P('')
P('## 7. 限界（計器と見積りの自己申告）')
P('- N は旗 0 の世界の上の影。移し替えを入れると闘争度の応答（共産主義の理想点は強権側＝立憲君主制・共和制で距離が伸びる正の帰還／絶対王政で負の帰還）で N は変わる（設計書 3-1 の予言は〈立憲君主制と共和制で組合が非合法の国は腕 B の N が影より大きい〉）。')
P('- セルの規模の参加の重みは思想の更新の後の断面（Factions.Step と同じ式）。時点差の大きさは 1. の不一致の割合。')
P('- 6. の c_L・勝ち率は旗 0 の基線の蜂起から取った。共産主義の束の蜂起の目標は PD（NearestPolity）で、現政体との距離・保持力・郷勇の応答は社会主義の蜂起（目標＝共和制）と別。')
print('\n'.join(out))
