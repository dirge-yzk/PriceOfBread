# -*- coding: utf-8 -*-
# (842) c68id 読み手：rule_s の段 P1-0 の 9 列から、P1 の母集合の件数を数える（計器の生だけを読む・エンジンを呼ばない）。
# 使い方：python rule_p1_count.py <計器の生のフォルダ（rule_s と secession_s がある）> [<出力ファイル>]
# 1 紙の台本 scripts/instruments/r838/p1_paper.py の数との突き合わせ（首都を除く・secession_s と同じ州の集合・m̄ は secession_s の crowdK を重みに m_r の人口加重の平均・R は遅れた R）：
#    国×四半期・州×四半期・R≧1 かつ R m̄/m_r≧1・R<1 かつ R m̄/m_r≧1・そのうち m̄/m_r<1・1 四半期前の R と publicOrderIndex（<1）の一致。紙の値と全て一致するはず（計器の列が紙と同じ値を出す確かめ）。
# 2 首都を含む母集合（計器の m̄＝首都を含む・R は遅れた R／今の R の 2 通り）：州×四半期・R≧1 かつ R m̄/m_r≧1・R<1 かつ R m̄/m_r≧1・そのうち m̄/m_r<1。
# 3 同じ四半期の一致の件数（反証役の訂正 8）：publicOrderIndex が 1 未満の国×四半期のうち、orderRawLagIndex が一致する数と、orderRawIndex（今の R）が一致する数。
# 4 目標の差：ungovTargetNewShare−ungovTargetOldShare と ungovTargetNewLagShare−ungovTargetOldShare の州の人口加重の平均（国×四半期）の分布。
import sys, os, math, statistics as st
sys.stdout.reconfigure(encoding='utf-8')
D = sys.argv[1]
OUT = sys.argv[2] if len(sys.argv) > 2 else None
SEEDS = range(1, 11)
lines = []
def P(*a):
    s = ' '.join(str(x) for x in a); lines.append(s); print(s)

def c01(x):
    return 0.0 if not (x > 0) else (1.0 if x > 1 else x)

def fl(x):
    return float('nan') if x == 'nan' else float(x)

def read(path, cols):
    with open(path, 'rb') as f:
        head = f.readline().decode('utf-8').rstrip('\r\n').split('\t')
        ix = [head.index(c) for c in cols]
        for raw in f:
            a = raw.decode('utf-8').rstrip('\r\n').split('\t')
            yield [a[i] for i in ix]

# 1 紙との突き合わせ
n_nq = n_reg = ctrl_ok = ctrl_low = ctrl_key = match_n = match_ok = 0
# 2 首都を含む
all_reg = {'lag': [0, 0, 0, 0], 'now': [0, 0, 0, 0]}   # [州×四半期, R≧1 かつ…≧1, R<1 かつ…≧1, m̄/m_r<1]
# 3 同じ四半期
po_n = po_lag = po_now = 0
# 4 目標の差
dnow, dlag = [], []
for s in SEEDS:
    rp = os.path.join(D, 'rule_s%d.tsv' % s); sp = os.path.join(D, 'secession_s%d.tsv' % s)
    if not (os.path.exists(rp) and os.path.exists(sp)):
        P('欠け 種', s); continue
    sec = {}; secq = set()
    for qt, nat, reg, crowd in read(sp, ['quarter', 'nation', 'region', 'crowdK']):
        sec[(qt, nat, reg)] = float(crowd); secq.add((int(qt), nat))
    groups = {}
    for a in read(rp, ['quarter', 'nation', 'region', 'popK', 'publicOrderIndex', 'orderRawIndex', 'orderRawLagIndex', 'militancyRegionIndex', 'militancyNationIndex',
                       'ungovTargetOldShare', 'ungovTargetNewShare', 'ungovTargetNewLagShare', 'reachShare']):
        groups.setdefault((a[0], a[1]), []).append(a)
    for (qt, nat), rows in groups.items():
        po = float(rows[0][4]); rnow = fl(rows[0][5]); rlag = fl(rows[0][6]); mbar = fl(rows[0][8])
        # 3 同じ四半期の一致（国×四半期に 1 回）
        if po < 1 - 1e-9:
            po_n += 1
            if not math.isnan(rlag) and abs(rlag - po) <= 1e-6 * max(1.0, abs(po)): po_lag += 1
            if not math.isnan(rnow) and abs(rnow - po) <= 1e-6 * max(1.0, abs(po)): po_now += 1
        # 2 首都を含む
        for key, R in (('lag', rlag), ('now', rnow)):
            if math.isnan(R): continue
            for a in rows:
                m = fl(a[7])
                if not (m > 0): continue
                all_reg[key][0] += 1
                if R * mbar / m >= 1:
                    if R >= 1:
                        all_reg[key][1] += 1
                        if mbar / m < 1: all_reg[key][3] += 1
                    else: all_reg[key][2] += 1
        # 4 目標の差（州の人口加重・Rule の長さが 4 の州だけ）
        pw = 0.0; sn = sl = 0.0
        for a in rows:
            pop = float(a[3]); o = fl(a[9]); n_ = fl(a[10]); l_ = fl(a[11])
            if math.isnan(o) or math.isnan(n_): continue
            pw += pop; sn += pop * (n_ - o)
            if not math.isnan(l_): sl += pop * (l_ - o)
        if pw > 0:
            dnow.append(sn / pw)
            if not math.isnan(rlag): dlag.append(sl / pw)
        # 1 紙との突き合わせ（secession_s と同じ州・首都を除く・R は遅れた R・m̄ は secession_s の crowdK を重みにした m_r の平均）
        sub = [(a, sec[(qt, nat, a[2])]) for a in rows if (qt, nat, a[2]) in sec]
        if not sub or math.isnan(rlag) or (int(qt) - 1, nat) not in secq: continue   # 紙の台本は前の四半期の secession_s の行がある国×四半期だけ（挙げた国の毎四半期）
        wsum = sum(c for _, c in sub)
        if not (wsum > 0): continue
        mb = sum(c * fl(a[7]) for a, c in sub) / wsum
        pw2 = sum(float(a[3]) for a, _ in sub)
        if not (pw2 > 0): continue
        n_nq += 1
        if po < 1 - 1e-9:
            match_n += 1
            if abs(rlag - po) <= 1e-6 * max(1.0, abs(po)): match_ok += 1
        for a, c in sub:
            m = fl(a[7])
            n_reg += 1
            if not (m > 0): continue
            if rlag * mb / m >= 1:
                if rlag >= 1:
                    ctrl_ok += 1
                    if mb / m < 1: ctrl_key += 1
                else: ctrl_low += 1

P('入力', D, '種 1〜10')
P('== 1 紙の台本との突き合わせ（首都を除く・secession_s と同じ州・遅れた R）。紙の値＝measurements/m842_20261105/paper_m839.txt：59813／930908／449843／64264／141882／27955')
P('  国×四半期 %d  州×四半期 %d  R≧1 かつ R m̄/m_r≧1 %d  R<1 かつ R m̄/m_r≧1 %d  そのうち m̄/m_r<1 %d  1 四半期前の R と publicOrderIndex(<1) の一致 %d / %d' % (n_nq, n_reg, ctrl_ok, ctrl_low, ctrl_key, match_ok, match_n))
P('== 2 首都を含む母集合（計器の m̄）')
for key, name in (('lag', '遅れた R'), ('now', '今の R')):
    a = all_reg[key]
    P('  %s：州×四半期（m_r>0）%d  R≧1 かつ R m̄/m_r≧1 %d  R<1 かつ R m̄/m_r≧1 %d  そのうち m̄/m_r<1 %d' % (name, a[0], a[1], a[2], a[3]))
P('== 3 同じ四半期の一致の件数：publicOrderIndex<1 の国×四半期 %d のうち 遅れた R が一致 %d・今の R が一致 %d' % (po_n, po_lag, po_now))
def q(v, p):
    s = sorted(v); return s[min(len(s) - 1, max(0, int(round(p * (len(s) - 1)))))] if s else float('nan')
for name, v in (('新−旧（今の R）', dnow), ('新−旧（遅れた R）', dlag)):
    P('== 4 目標の差 %s 国×四半期 %d  p10 %.4g  中央 %.4g  p90 %.4g  平均 %.4g' % (name, len(v), q(v, .1), q(v, .5), q(v, .9), st.mean(v) if v else float('nan')))
if OUT:
    with open(OUT, 'w', encoding='utf-8', newline='\n') as f: f.write('\n'.join(lines) + '\n')
