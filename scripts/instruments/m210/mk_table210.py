"""(210) 表 measurements/f210_20261004 を作る（読み手の出力を並べるだけ）。使い方：PYTHONUTF8=1 python mk_table210.py <作業フォルダ> <出力>"""
import sys, os, re, statistics as st
W, OUT = sys.argv[1], sys.argv[2]
SP = os.path.dirname(os.path.abspath(__file__))
def rd(p): return open(p, encoding='utf-8').read().rstrip('\n') + '\n'
def per(p):
    d = {}
    for l in open(p, encoding='utf-8'):
        m = re.match(r'\s+seed(\d+)\s+exact ([\d.]+)\s+near ([\d.]+)', l)
        if m: d[int(m.group(1))] = (float(m.group(2)), float(m.group(3)))
    return d
o = []
o.append('# (210) 線F 前半＝(205) の exact −4.6 の帰属と不履行の頻度の単位・2026-10-04\n')
o.append('# 木＝腕A r207（4f73d13＝main・SOURCE-HASH 000feb3ddac75b1c）と r202（62cc878・7d53564228a15fd1）の写し（git archive）に読むだけの計器 scratchpad/m210/inj_d210.py を当てた（計器つきの SOURCE-HASH は下の名乗り）。\n')
o.append('# 帯＝種1..10（計器つき＝基線 b10_20261004_r207・b10_20261004_r202 と全行バイト一致＝写していない）・種11..20 と 21..30（計器つき＝b10_20261004_r207s11・r207s21・r202s11・r202s21）。\n')
o.append('# 読み手＝scratchpad/m210/an_d210.py（exact の帰属）・an_ev210.py（不履行の出来事）・an_rep210.py（国ごとの差の再現）・an_pol210.py（政体の分布）・an_grp210.py（国の群）・an_ts210.py（群の時系列）・an_pd210.py（不履行の頻度を同じ単位で）・NG の集合＝scratchpad/m192/ngset192.py。\n')
o.append('# 計器の行の形＝inj_d210.py の頭書き（Q 行・D 行・N 行）。\n\n')
o.append('## 1. 名乗り（SOURCE-HASH）と報告の一致\n')
for tag in ('A207_10', 'A202_10', 'A207_10_s11', 'A202_10_s11', 'A207_10_s21', 'A202_10_s21'):
    m = open(os.path.join(W, f'b10_{tag}.txt.manifest'), encoding='utf-8').read().splitlines()
    h = open(os.path.join(W, f'b10_{tag}.txt'), encoding='utf-8').readline().strip()
    o.append(f'- {tag}: {m[0]}・{m[2]}・{h}\n')
o.append('- 種1..10 の計器つきの報告＝基線と全行バイト一致（A207_10＝b10_20261004_r207 sha1 2aa1adc154ed・A202_10＝b10_20261004_r202 sha1 152842f09eab）＝計器は読むだけ。\n\n')
o.append('## 2. exact と near の種ごとの値（.cost の忠実度の種ごとの値）\n')
a = {}; b = {}
for s in ('', '_s11', '_s21'):
    a.update(per(os.path.join(W, f'b10_A202_10{s}.txt.cost'))); b.update(per(os.path.join(W, f'b10_A207_10{s}.txt.cost')))
o.append('| 種 | r202 exact | r207 exact | 差 | r202 near | r207 near | 差 |\n|---|---|---|---|---|---|---|\n')
for s in range(1, 31):
    o.append(f'| {s} | {a[s][0]} | {b[s][0]} | {b[s][0]-a[s][0]:+.1f} | {a[s][1]} | {b[s][1]} | {b[s][1]-a[s][1]:+.1f} |\n')
for lo, hi in ((1, 10), (11, 20), (21, 30), (1, 20), (1, 30)):
    r = range(lo, hi + 1); ds = [b[s][0] - a[s][0] for s in r]; dn = [b[s][1] - a[s][1] for s in r]; n = len(ds)
    o.append(f'- 種 {lo}..{hi}: r202 exact {st.mean(a[s][0] for s in r):.2f}・near {st.mean(a[s][1] for s in r):.2f}／r207 exact {st.mean(b[s][0] for s in r):.2f}・near {st.mean(b[s][1] for s in r):.2f}／差 exact {st.mean(ds):+.2f}（標準誤差 {st.stdev(ds)/n**0.5:.2f}）・near {st.mean(dn):+.2f}（{st.stdev(dn)/n**0.5:.2f}）\n')
o.append('\n')
for g, lab in (('s1', '種1..10'), ('s11', '種11..20'), ('s21', '種21..30')):
    o.append(f'## 3. exact の帰属（an_d210・{lab}）\n'); o.append(rd(os.path.join(W, 'rd', f'd210_{g}.txt'))); o.append('\n')
for g, lab in (('s1', '種1..10'), ('s11', '種11..20'), ('s21', '種21..30')):
    o.append(f'## 4. 不履行の出来事（an_ev210・{lab}）\n'); o.append(rd(os.path.join(W, 'rd', f'ev_{g}.txt'))); o.append('\n')
o.append('## 5. 国ごとの exact の差の再現（an_rep210・3つの種の組）\n'); o.append(rd(os.path.join(W, 'rd', 'rep.txt'))); o.append('\n')
o.append('## 6. 1914 年の導出政体の分布・インドの諸邦（an_pol210・an_grp210・an_ts210）\n'); o.append(rd(os.path.join(W, 'rd', 'pol.txt'))); o.append('\n')
o.append('## 7. 不履行の頻度を同じ単位で（an_pd210）\n'); o.append(rd(os.path.join(W, 'rd', 'pd_sum.txt'))); o.append('\n')
for arm in ('A207', 'A202'):
    for g, lab in (('s1', '種1..10'), ('s11', '種11..20'), ('s21', '種21..30')):
        o.append(f'### 7-{arm}-{g}（{lab}）\n'); o.append(rd(os.path.join(W, 'rd', f'pd_{arm}_{g}.txt'))); o.append('\n')
o.append(rd(os.path.join(SP, 'rr_nature210.txt'))); o.append('\n')
o.append('## 9. 史実比較の桁の NG の集合（ngset192・基準＝表 m207_20261004 の FI10・比べる＝同じ FI10＝コードを入れていない）\n'); o.append(rd(os.path.join(W, 'rd', 'ng.txt')))
open(OUT, 'w', encoding='utf-8', newline='\n').write(''.join(o))
print('ok', OUT)
