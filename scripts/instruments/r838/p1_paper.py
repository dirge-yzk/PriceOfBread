# -*- coding: utf-8 -*-
# (838) c67ds 読むだけの台本：州ごとの治安 P1（相対の被覆 R×m̄÷m_r）の紙の見積もり。
# 入力＝基線の計器の生（keep_832/base_ins の secession_s・rule_s・種 1〜10）。エンジンを呼ばない・世界へ書かない。
# 出力＝標準出力（呼び手が UTF-8 のファイルへ書く）。
#   0 単位の突き合わせ：R_raw＝coerceIndex÷(totalPopK×0.8) と rule_s の publicOrderIndex（<1 の行）の一致。
#   1 低③（イェンセン）：国×四半期の州の人口加重の平均 Clamp01(R m̄/m_r) − Clamp01(R)（P1 と P1′〔m̄ を調和平均〕）。
#   2 中①：対照の母集合〈R≧1 かつ R m̄/m_r≧1〉と〈R<1 かつ R m̄/m_r≧1〉の州×四半期の件数。
#   3 旗 0 の〈働いた〉の値：同じ国×四半期の m_r 上位 1/4 と下位 1/4 の州の ruleUngovShare の平均の差（比にしない）・reach の差も。
#   4 m_r＝0 の州の割合。
#   5 目標の無統治の差 ΔtUngov＝(Clamp01(R)−order_r)×(1−reach) の州の人口加重の平均（国×四半期）。
# 限界（自己申告）：secession_s は首都と 1 州の国を持たない＝m̄ は〈首都を除く・大衆の人口加重〉・首都の州の P1 は測れない。
import sys, os, math, statistics as st
sys.stdout.reconfigure(encoding='utf-8')

BASE = sys.argv[1] if len(sys.argv) > 1 else 'D:/PoB_work/keep_832/base_ins'
SEEDS = range(1, 11)
ORDER_PER_POP = 0.8   # Coefficients.CrimeOrderPerPop の既定（Crime.cs:20 と同じ式）

def c01(x):
    return 0.0 if not (x > 0) else (1.0 if x > 1 else x)

def q(vals, p):
    if not vals: return float('nan')
    s = sorted(vals); i = min(len(s) - 1, max(0, int(round(p * (len(s) - 1)))))
    return s[i]

def fmt(x):
    return 'nan' if x != x else ('%.4g' % x)

def read(path, cols):
    with open(path, 'rb') as f:
        head = f.readline().decode('utf-8').rstrip('\r\n').split('\t')
        ix = [head.index(c) for c in cols]
        for raw in f:
            a = raw.decode('utf-8').rstrip('\r\n').split('\t')
            yield [a[i] for i in ix]

match_n = match_ok = 0
jen1, jen2, dtu = [], [], []
ctrl_ok = ctrl_low = ctrl_all = 0
ctrl_key = 0   # R>1 かつ m̄/m_r<1 かつ R m̄/m_r≧1＝〈警察が十分なら闘争度が高くても効かない〉を切った R で書く誤りが落ちる母集合
work_d, reach_d = [], []
zero_n = reg_n = 0
nq_n = 0
for s in SEEDS:
    rp = os.path.join(BASE, 'rule_s%d.tsv' % s); sp = os.path.join(BASE, 'secession_s%d.tsv' % s)
    if not (os.path.exists(rp) and os.path.exists(sp)): print('欠け 種', s); continue
    rule = {}
    for qt, nat, reg, pop, ung, rch, po in read(rp, ['quarter', 'nation', 'region', 'popK', 'ruleUngovShare', 'reachShare', 'publicOrderIndex']):
        rule[(qt, nat, reg)] = (float(pop), float(ung), float(rch), float(po))
    groups = {}
    for qt, nat, reg, coe, tp, crowd, mil in read(sp, ['quarter', 'nation', 'region', 'coerceIndex', 'totalPopK', 'crowdK', 'militancyMeanIndex']):
        k = (qt, nat, reg)
        if k not in rule: continue
        groups.setdefault((qt, nat), []).append((float(coe), float(tp), float(crowd), float(mil), rule[k]))
    # ★単位の突き合わせで分かった遅れ：rule_s の publicOrderIndex（＝Rule の目標が読む w.PublicOrder）は、前の四半期の secession_s の R と一致する
    #   （Crime.Step が前の四半期の Coercion で秩序を書き、CheckSecession の Rule のループがそれを読む）＝R は 1 四半期前の値を使う。
    Rq = {}
    for (qt, nat), rows in groups.items():
        coe, tp = rows[0][0], rows[0][1]
        if tp > 0: Rq[(int(qt), nat)] = coe / (tp * ORDER_PER_POP)
    for (qt, nat), rows in groups.items():
        R = Rq.get((int(qt) - 1, nat))
        if R is None: continue
        po = rows[0][4][3]
        if po < 1 - 1e-9:
            match_n += 1
            if abs(R - po) <= 1e-6 * max(1.0, abs(po)): match_ok += 1
        wsum = sum(r[2] for r in rows)
        if not (wsum > 0): continue
        mbar = sum(r[2] * r[3] for r in rows) / wsum
        pos = [r for r in rows if r[3] > 0]
        mh = (sum(r[2] for r in pos) / sum(r[2] / r[3] for r in pos)) if pos and len(pos) == len(rows) else 0.0
        nq_n += 1
        o0 = c01(R)
        pw = sum(r[4][0] for r in rows)
        if not (pw > 0): continue
        a1 = a2 = d = 0.0
        for coe_, tp_, crowd, m, (pop, ung, rch, po_) in rows:
            reg_n += 1
            if not (m > 0): zero_n += 1; o1 = 1.0; o2 = 1.0
            else:
                o1 = c01(R * mbar / m); o2 = c01(R * mh / m)
                ctrl_all += 1
                if R * mbar / m >= 1:
                    if R >= 1:
                        ctrl_ok += 1
                        if mbar / m < 1: ctrl_key += 1
                    else: ctrl_low += 1
            a1 += pop * o1; a2 += pop * o2; d += pop * (o0 - o1) * (1 - rch)
        jen1.append(a1 / pw - o0); jen2.append(a2 / pw - o0); dtu.append(d / pw)
        if len(rows) >= 4:
            srt = sorted(rows, key=lambda r: r[3]); k4 = max(1, len(srt) // 4)
            lo, hi = srt[:k4], srt[-k4:]
            work_d.append(st.mean(r[4][1] for r in hi) - st.mean(r[4][1] for r in lo))
            reach_d.append(st.mean(r[4][2] for r in hi) - st.mean(r[4][2] for r in lo))

print('入力', BASE, '種 1〜10・国×四半期（2 州以上の国・首都を除く州）', nq_n, '州×四半期', reg_n)
print('== 0 単位の突き合わせ：1 四半期前の R_raw と publicOrderIndex（<1）の一致', match_ok, '/', match_n)
def line(name, v):
    pos = sum(1 for x in v if x > 1e-12); neg = sum(1 for x in v if x < -1e-12)
    print('  %s  件 %d  p10 %s  中央 %s  p90 %s  平均 %s  正 %d  負 %d' % (name, len(v), fmt(q(v, .1)), fmt(q(v, .5)), fmt(q(v, .9)), fmt(st.mean(v) if v else float('nan')), pos, neg))
print('== 1 低③ 州の人口加重の平均の秩序の差（P1 − 旗 0）と（P1′〔調和平均〕− 旗 0）')
line('P1 ', jen1); line('P1′', jen2)
print('== 2 中① 対照の母集合（m_r>0 の州×四半期 %d）' % ctrl_all)
print('  R≧1 かつ R m̄/m_r≧1 :', ctrl_ok, '  R<1 かつ R m̄/m_r≧1（P1 で秩序が 1 になり目標が動く州）:', ctrl_low)
print('  そのうち m̄/m_r<1（闘争度が国の平均より高いが警察が十分な州）:', ctrl_key)
print('== 3 旗 0 の〈働いた〉の値：同じ国×四半期の m_r 上位 1/4 − 下位 1/4（4 州以上の国）')
line('ruleUngovShare の差', work_d); line('reachShare の差     ', reach_d)
print('== 4 m_r＝0 の州×四半期', zero_n, '/', reg_n)
print('== 5 目標の無統治の差 ΔtUngov＝(Clamp01(R)−order_r)×(1−reach) の州の人口加重の平均（正＝P1 で無統治の目標が上がる）')
line('ΔtUngov', dtu)
