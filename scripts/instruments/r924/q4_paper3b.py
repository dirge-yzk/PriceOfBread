# (924) 問い4：(3b)〔②′ の lean を軍の帰属 ArmyAllegiance へ〕の紙の当て（今の軌道の上の一歩・動学は追わない）
# 新②′_t = −ArmyPcPressure×max(0, 帰属_t)×軍の政治力_t。帰属_t＝次の行の armyAllegiancePrevIndex（エンジンの :208 が同じ四半期に書いた値＝(3b) が :139 で読む値と同じ入力〔lean と milAff は :85・:90 で既に出ている〕）。
# 旧②′ の検算：−0.02×max(0,lean)×armyPower と term2pShare の差。
# 使い方: python -X utf8 -I q4_paper3b.py <out>
import csv, sys, math, os
csv.field_size_limit(10**8)
ROOT = 'D:/PoB_work/keep_919'
ARMS = [('base', 'base_ins', range(1, 11)), ('n1', 'n1', range(1, 11)), ('n2', 'n2', range(1, 11)), ('a11', 'a11', range(11, 21))]
NATS = ['イギリス', '清', 'ロシア', 'プロイセン', '日本', 'フランス', 'オーストリア', 'オスマン']
K = 0.02
def f(x):
    try: return float(x)
    except Exception: return math.nan
out = open(sys.argv[1], 'w', encoding='utf-8', newline='\n')
out.write('arm\tnation\t立憲の行\t旧②′Σ\t新②′Σ\t新÷旧\t旧が負の行\t新が負の行\t旧0→新負\t旧負→新0\tPC<0.5 の行の新÷旧\tPC≥0.5 の行の新÷旧\t検算の最大差\t王冠=専制の立憲の行の新÷旧\n')
for arm, sub, seeds in ARMS:
    acc = {}
    for s in seeds:
        rows = {}
        with open(os.path.join(ROOT, sub, f'crownpc_s{s}.tsv'), encoding='utf-8', newline='') as fh:
            for r in csv.DictReader(fh, delimiter='\t'):
                if r['nation'] in NATS: rows.setdefault(r['nation'], []).append(r)
        for n, rs in rows.items():
            a = acc.setdefault(n, dict(cm=0, old=0.0, new=0.0, on=0, nn=0, off2neg=0, neg2off=0, lo_o=0.0, lo_n=0.0, hi_o=0.0, hi_n=0.0, chk=0.0, d_o=0.0, d_n=0.0))
            for i in range(len(rs) - 1):
                r = rs[i]; nx = rs[i + 1]
                if r['polityCode'] != '3' or r['seizureCode'] != '0': continue
                if int(nx['quarter']) != int(r['quarter']) + 1: continue
                old = f(r['term2pShare']); lean = f(r['soldierLeanIndex']); ap = f(r['armyPowerIndex']); al = f(nx['armyAllegiancePrevIndex']); pc = f(r['pcPrevShare'])
                if any(math.isnan(v) for v in (old, lean, ap, al, pc)): continue
                chk = -K * max(0.0, lean) * ap
                a['chk'] = max(a['chk'], abs(chk - old))
                new = -K * max(0.0, al) * ap
                a['cm'] += 1; a['old'] += old; a['new'] += new
                a['on'] += old < 0; a['nn'] += new < 0
                a['off2neg'] += (not old < 0) and new < 0; a['neg2off'] += old < 0 and not new < 0
                if pc < 0.5: a['lo_o'] += old; a['lo_n'] += new
                else: a['hi_o'] += old; a['hi_n'] += new
                if r['crownCode'] == '2': a['d_o'] += old; a['d_n'] += new
    for n in NATS:
        a = acc.get(n)
        if not a: continue
        rat = lambda x, y: f'{y / x:.3f}' if x != 0 else 'nan'
        out.write(f"{arm}\t{n}\t{a['cm']}\t{a['old']:.3f}\t{a['new']:.3f}\t{rat(a['old'], a['new'])}\t{a['on']}\t{a['nn']}\t{a['off2neg']}\t{a['neg2off']}\t{rat(a['lo_o'], a['lo_n'])}\t{rat(a['hi_o'], a['hi_n'])}\t{a['chk']:.2e}\t{rat(a['d_o'], a['d_n'])}\n")
out.close()
