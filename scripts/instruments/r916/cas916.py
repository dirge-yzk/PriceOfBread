# (916) 読むだけ：casualty_s（年×国の戦った月・兵の月の和・戦死）から兵 1 人の月あたりの戦死の割合を出し、
#   内戦の側（wars_s の civilWarFlag=1 の両側の国名・開始の年から months/12 年の間）と対外戦の側に分けて分布を並べる。
#   戦死の割合 ÷ WarCasualtyRatePerMonth（0.002÷WorkingMenStdShare）＝接触の割合（contact÷Military）×制空の増し（上限 0.04 で頭打ち）の代理。
#   使い方: python -X utf8 -I cas916.py <ins> [種の起点] [WorkingMenStdShare の値]
import sys, os, statistics as st
M = sys.argv[1]; SB = int(sys.argv[2]) if len(sys.argv) > 2 else 1
WMS = float(sys.argv[3]) if len(sys.argv) > 3 else float('nan')
RATE = 0.002 / WMS if WMS == WMS else float('nan')
civ = []; fo = []; civ0 = 0; fo0 = 0
for s in range(SB, SB + 10):
    pc = os.path.join(M, f'casualty_s{s}.tsv'); pw = os.path.join(M, f'wars_s{s}.tsv')
    if not (os.path.exists(pc) and os.path.exists(pw)): print('欠け', s); continue
    cw = set()
    with open(pw, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t')
        for l in f:
            r = l.rstrip('\n').split('\t')
            if r[h.index('civilWarFlag')] != '1': continue
            y0 = int(r[h.index('startYear')]); mo = int(r[h.index('months')])
            for y in range(y0, y0 + mo // 12 + 1):
                cw.add((y, r[h.index('sideA')])); cw.add((y, r[h.index('sideB')]))
    with open(pc, encoding='utf-8') as f:
        f.readline()
        for l in f:
            y, n, mo, sm, dk = l.rstrip('\n').split('\t')
            sm = float(sm); dk = float(dk)
            if not (sm > 0): continue
            x = dk / sm   # 兵 1 人の月あたりの戦死の割合
            if (int(y), n) in cw:
                civ.append(x); civ0 += (x < 1e-6)
            else:
                fo.append(x); fo0 += (x < 1e-6)
def q(v, p): return sorted(v)[min(len(v) - 1, int(p * len(v)))] if v else float('nan')
for name, v, z in (('内戦の側', civ, civ0), ('対外戦の側', fo, fo0)):
    if not v: print(name, '0 行'); continue
    print(f'{name}\t国×年 {len(v)}\t戦死ほぼ 0（<1e-6）{z}\t中央 {st.median(v):.5f}\t平均 {st.mean(v):.5f}\tp90 {q(v,0.9):.5f}\t最大 {max(v):.5f}\t上限 0.04 に届く {sum(1 for x in v if x >= 0.04 - 1e-9)}'
          + (f'\t接触の代理の中央 {st.median(v)/RATE:.4f}・平均 {st.mean(v)/RATE:.4f}' if RATE == RATE else ''))
