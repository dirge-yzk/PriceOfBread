"""(186) 兌換停止の読み手（読むだけ）。python an_gold186.py <out_dir/g186> [国の BaseName=イギリス]
S 行（兌換停止の瞬間）＝引き金（財政不足 r<0.7・カバー率割れ）×交戦・総動員・軍の請求・利払い÷歳入・国債÷年GDP・国庫・切り下げの記憶。
G 行（国×四半期）＝金本位でない月の再加入の門の閉じ方（r<0.85・カバー率<0.325・切り下げの記憶 broken）。"""
import sys, os, re, statistics as st
from collections import Counter, defaultdict
d = sys.argv[1]; NAT = sys.argv[2] if len(sys.argv) > 2 else 'イギリス'
S = []; G = defaultdict(list)   # seed -> rows
allS = []
for fn in sorted(os.listdir(d), key=lambda x: int(re.sub(r'\D', '', x) or 0)):
    m = re.match(r'g(\d+)\.txt$', fn)
    if not m: continue
    seed = int(m.group(1))
    for ln in open(os.path.join(d, fn), encoding='utf-8'):
        p = ln.rstrip('\n').split('|')
        if p[0] == 'S':
            q, idx, yr, name = int(p[1]), int(p[2]), int(p[3]), p[4]
            v = [float(x) for x in p[5:]]
            rec = dict(seed=seed, q=q, yr=yr, name=name, r=v[0], cov=v[1], covMin=v[2], fund=v[3], cover=v[4], war=v[5], mob=v[6], milSp=v[7],
                       mil=v[8], adm=v[9], intr=v[10], rev=v[11], treas=v[12], ag=v[13], debt=v[14], deval=v[15], broken=v[16], cdr=v[17],
                       base=v[18], broad=v[19], loans=v[20], conv=v[21], parl=v[22])
            allS.append(rec)
            if name == NAT: S.append(rec)
        elif p[0] == 'G' and p[4] == NAT:
            G[seed].append([int(p[1]), int(p[2])] + [float(x) for x in p[5:]])
seeds = sorted(G)
print(f'# 兌換停止の診断（{NAT}・{len(seeds)}種）＝計器 inj_g186（読むだけ）')
print(f'## 1. {NAT} の兌換停止 {len(S)} 回（1種あたり {len(S) / max(1, len(seeds)):.1f}）')
def trig(r): return '両方' if r['fund'] and r['cover'] else '財政不足（r<0.7）' if r['fund'] else 'カバー率割れ' if r['cover'] else '？'
c = Counter(trig(r) for r in S); print('  引き金：' + '・'.join(f'{k} {v}' for k, v in c.most_common()))
print(f"  交戦中 {sum(1 for r in S if r['war'])}・総動員 {sum(1 for r in S if r['mob'])}・切り下げの記憶 broken {sum(1 for r in S if r['broken'])}")
dec = Counter((r['yr'] // 10) * 10 for r in S); print('  年代：' + ' '.join(f'{k}s {v}' for k, v in sorted(dec.items())))
print('  種 | 年 | 引き金 | r | カバー率 | 交戦 | 総動員 | 軍の請求÷歳入 | 利払い÷歳入 | 国債÷年GDP | 国庫÷年GDP | 与信÷広義 | 兌換 | 議会')
for r in sorted(S, key=lambda x: (x['seed'], x['q'])):
    rev = r['rev'] if r['rev'] > 0 else float('nan')
    print(f"  {r['seed']} | {r['yr']} | {trig(r)} | {r['r']:.3f} | {r['cov']:.3f} | {int(r['war'])} | {int(r['mob'])} | {r['mil'] / rev:.2f} | {r['intr'] / rev:.2f} | "
          f"{r['debt'] / r['ag'] if r['ag'] > 0 else float('nan'):.2f} | {r['treas'] / r['ag'] if r['ag'] > 0 else float('nan'):.3f} | {r['loans'] / r['broad'] if r['broad'] > 0 else float('nan'):.2f} | {r['conv']:.2f} | {r['parl']:.2f}")
# 引き金の中身（財政不足の月の r と、カバー率割れの月の与信）
fund = [r for r in S if r['fund']]; cov = [r for r in S if r['cover']]
if fund:
    print(f"  財政不足の月：r の中央 {st.median(r['r'] for r in fund):.3f}・軍の請求÷歳入の中央 {st.median(r['mil'] / r['rev'] for r in fund if r['rev'] > 0):.2f}・利払い÷歳入の中央 {st.median(r['intr'] / r['rev'] for r in fund if r['rev'] > 0):.2f}・交戦 {sum(1 for r in fund if r['war'])}/{len(fund)}")
if cov:
    print(f"  カバー率割れの月：カバー率の中央 {st.median(r['cov'] for r in cov):.3f}・与信÷広義の中央 {st.median(r['loans'] / r['broad'] for r in cov if r['broad'] > 0):.2f}・国庫÷年GDP の中央 {st.median(r['treas'] / r['ag'] for r in cov if r['ag'] > 0):.3f}・交戦 {sum(1 for r in cov if r['war'])}/{len(cov)}")
# 2. 金本位でない期間と、戻れない理由
print(f'## 2. {NAT} の金本位の期間（G 行・月の和）')
cols = ['months', 'goldM', 'lawM', 'offLawM', 'failR', 'failCov', 'failBroken', 'rSum', 'covSum', 'convSum', 'warM', 'susp', 'allOpenM']
def agg(rows):
    t = defaultdict(float)
    for row in rows:
        for k, name in enumerate(cols): t[name] += row[2 + k]
    return t
print('  窓 | 金本位の月の割合（種の中央）| 法がある月のうち金本位でない割合 | 戻れない月の理由（r<0.85・カバー率<0.325・broken の割合＝重なりあり）| 停止の回数（10種の和）')
for y0, y1 in [(1783, 1796), (1797, 1821), (1822, 1849), (1850, 1879), (1880, 1913), (1914, 1930), (1931, 1949)]:
    shares = []; T = defaultdict(float)
    for s in seeds:
        rows = [row for row in G[s] if y0 <= row[1] <= y1]
        t = agg(rows)
        if t['months'] > 0: shares.append(t['goldM'] / t['months'])
        for k, v in t.items(): T[k] += v
    if not shares: continue
    off = T['offLawM']
    rs = f"{T['failR'] / off:.2f}・{T['failCov'] / off:.2f}・{T['failBroken'] / off:.2f}" if off > 0 else '—'
    print(f"  {y0}-{y1} | {st.median(shares):.2f}（{min(shares):.2f}〜{max(shares):.2f}）| {off / T['lawM'] if T['lawM'] > 0 else float('nan'):.2f} | {rs} | {int(T['susp'])}")
# 金本位を離れていた期間の長さ（連続した四半期）
spells = []
for s in seeds:
    run = 0
    for row in G[s]:
        t = dict(zip(cols, row[2:]))
        if t['offLawM'] >= 3: run += 1
        else:
            if run > 0: spells.append(run)
            run = 0
    if run > 0: spells.append(run)
if spells:
    print(f'  金属本位の法のまま金本位を離れた連続の期間（四半期）：{len(spells)} 本・中央 {st.median(spells):.0f}・最長 {max(spells)}・合計 {sum(spells)}（10種）')
# 3. 全ての国
print(f'## 3. 全ての国の兌換停止 {len(allS)} 回（帯の報告の「総…回」は再併合で消えた国の分を数えない）')
c = Counter(trig(r) for r in allS); print('  引き金：' + '・'.join(f'{k} {v}' for k, v in c.most_common()))
print(f"  交戦中 {sum(1 for r in allS if r['war'])}・broken {sum(1 for r in allS if r['broken'])}")
byn = Counter(r['name'] for r in allS); print('  国（上位）：' + '・'.join(f'{k} {v}' for k, v in byn.most_common(12)))
