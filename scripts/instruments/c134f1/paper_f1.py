# (1296) c134f1 紙の台本（読むだけ・腕なし）：旗 UrbanFlowReal を 1 にする前の盤面（keep_1294/base_ins）で、
#   開幕の値打ちの g（urxpd_s の wGsym÷sumTotal・本国格の州・計器の 9 国）と、旗 1 の釣り合いの対の都市の割合 σ〈推〉との差・
#   開幕の一斉の過渡〈推〉の大きさを国×年代で並べる。
#   使い方: python -X utf8 -I paper_f1.py <ins（D:/…）> <種の起点>
#   σ〈推〉：対の稼ぎの総額が移っても変わらないと置いた近似（V_u÷V_r＝(2+g)/(2−g)＝k・今の対の都市の割合 s）＝ s*＝s·k÷(s·k＋1−s)。
#   過渡〈推〉：旗 1 の最初の 12 か月に都市から農村へ移る割合＝1−(1−Mobility×|g|)^12（Mobility 0.02・g＜0 のとき・送り手＝都市）。
#   旗 0（今の式）で門が落ちることの確かめ：今の式の流れ（sumFlowPos−sumFlowNeg）の向きと、g の向きが逆の国×年代の数（＝旗 1 で向きが変わる所）。
import sys, csv, os, statistics as st, collections
NAT = ['イギリス', 'フランス', 'プロイセン', 'アメリカ', '日本', '清', 'ロシア', 'オスマン', 'オーストリア']
WIN = [(1783, 1783), (1784, 1814), (1815, 1849), (1850, 1879), (1880, 1913), (1914, 1950)]
MOB = 0.02
d = sys.argv[1]; sb = int(sys.argv[2])
acc = collections.defaultdict(lambda: collections.defaultdict(list))   # (nation, win) -> col -> [per-seed]
for s in range(sb, sb + 10):
    p = os.path.join(d, f'urxpd_s{s}.tsv')
    if not os.path.exists(p): continue
    per = collections.defaultdict(lambda: collections.defaultdict(float))
    with open(p, encoding='utf-8') as f:
        for r in csv.DictReader(f, delimiter='\t'):
            y = int(r['year']); n = r['nation']
            if n not in NAT: continue
            w = next((f'{a}-{b}' for a, b in WIN if a <= y <= b), None)
            if w is None: continue
            for k in ('regionMonths', 'famineGated', 'sumFlowPos', 'sumFlowNeg', 'sumTotal', 'sumPairUrban', 'wGsym', 'wAbsGsym', 'wTargetPre', 'wCur'):
                per[(n, w)][k] += float(r[k])
    for key, v in per.items():
        T = v['sumTotal']
        if T <= 0: continue
        g = v['wGsym'] / T; s0 = v['sumPairUrban'] / T; k = (2 + g) / (2 - g)
        sig = s0 * k / (s0 * k + 1 - s0)
        tr = 1 - (1 - MOB * abs(g)) ** 12 if g < 0 else 0.0
        a = acc[key]
        a['g'].append(g); a['absg'].append(v['wAbsGsym'] / T); a['s'].append(s0); a['sig'].append(sig); a['sig_minus_s'].append(sig - s0)
        a['tgt_minus_s'].append((v['wTargetPre'] - v['wCur']) / T); a['tr12'].append(tr)
        a['net0'].append(v['sumFlowPos'] - v['sumFlowNeg']); a['gated'].append(v['famineGated'] / max(1, v['regionMonths']))
        a['opp'].append(1 if (v['sumFlowPos'] - v['sumFlowNeg']) * g < 0 else 0)
cols = ['g', 'absg', 's', 'sig', 'sig_minus_s', 'tgt_minus_s', 'tr12', 'gated', 'net0']
print('# 種の中央値・本国格の州・計器の 9 国。g＝値打ちの対称の相対差（旗 0 の盤面の値）・s＝対の都市の割合・sig＝旗 1 の釣り合い〈推〉・tr12＝旗 1 の最初の 12 か月に都市から農村へ移る対の都市の割合〈推〉')
print('nation\twindow\tseeds\t' + '\t'.join(cols) + '\toppSeeds')
nopp = 0; ncell = 0
for n in NAT:
    for a0, b0 in WIN:
        w = f'{a0}-{b0}'; a = acc.get((n, w))
        if not a: continue
        ncell += 1; o = sum(a['opp']); nopp += 1 if o * 2 > len(a['opp']) else 0
        print(f'{n}\t{w}\t{len(a["g"])}\t' + '\t'.join(f'{st.median(a[c]):.4f}' for c in cols) + f'\t{o}')
print(f'# 旗 0 の今の式の純の流れの向きと g の向きが逆の国×年代（種の過半）: {nopp} / {ncell}')
