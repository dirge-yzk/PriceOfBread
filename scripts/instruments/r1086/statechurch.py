# (1086) 読むだけ：国教の法（monlaw_s の StateReligion）と移民制限法の分布、異教の流れの量と、国教のみの国へ入る割合。
# 使い方：python -X utf8 -I statechurch.py <base_ins> <world.json> <種の数>
# 限界（自己申告）：
#  - 〈異教〉＝出る州の宗教の札 ≠ 行き先の国の主流の宗教（開幕の州の割合で加重した多数派＝Diplomacy.cs:89 の毎月の同期の近似・途中の州の得失は見ない）。
#  - 出る州の札が無い（植民地格など world.json に無い州・空の札）流れは〈札なし〉に分ける。
#  - xmig_s は受入の上限で縮めた後・Pop.Blend の切り詰めの前の量（flows.py の頭書きと同じ）。
#  - 在シミュの州の札は world.json と違いうる：植民地格の州は Simulation.cs の PromoteColonyToRegion で宗主国の首都の札を受け継ぐ。この台本は world.json に無い州を〈札なし〉にする。
import sys, json, collections as C

base, wj, nseed = sys.argv[1], sys.argv[2], int(sys.argv[3])
W = json.load(open(wj, encoding='utf-8'))
reg = {}; natrel = C.defaultdict(C.Counter)
for x in W['nations']:
    for p in x['provinces']:
        reg[p['city']] = p.get('religion', '')
        natrel[x['name']][p.get('religion', '')] += p.get('share', 0.0)
mainrel = {n: max(c.items(), key=lambda kv: kv[1])[0] for n, c in natrel.items()}
LV = ['国教のみ', '内心の自由', '信仰の自由', '国家無神論']
IM = ['開放', '選別', '閉鎖']

dist = C.Counter(); imd = C.Counter(); us = {}
law = {}   # (種, 四半期, 国) -> 国教の法
cls = C.Counter()
for s in range(1, nseed + 1):
    f = open(f'{base}/monlaw_s{s}.tsv', encoding='utf-8')
    h = f.readline().rstrip('\n').split('\t')
    iq, inn, isr, iim = h.index('quarter'), h.index('nation'), h.index('StateReligion'), h.index('ImmigrationRestriction')
    for line in f:
        c = line.rstrip('\n').split('\t')
        q = int(c[iq]); sr = int(c[isr]); im = int(c[iim])
        law[(s, q, c[inn])] = sr
        if q % 40 == 0:
            dist[(1783 + q // 4, sr)] += 1; imd[(1783 + q // 4, im)] += 1
            if c[inn] in ('アメリカ', 'イギリス', '清', '日本', 'ロシア') and s == 1:
                us[(c[inn], 1783 + q // 4)] = (sr, im)
    f.close()
    g = open(f'{base}/xmig_s{s}.tsv', encoding='utf-8')
    h = g.readline().rstrip('\n').split('\t')
    iq, ifr, itn, iv = h.index('quarter'), h.index('fromRegion'), h.index('toNation'), h.index('movedKPerQuarter')
    for line in g:
        c = line.rstrip('\n').split('\t')
        v = float(c[iv])
        if not (v > 0): continue
        q = int(c[iq]); r = reg.get(c[ifr], ''); tn = c[itn]
        if not r: k = '札なし'
        elif r == mainrel.get(tn, ''): k = '同じ'
        else: k = '異教'
        sr = law.get((s, q, tn), -1)
        cls[(k, LV[sr] if sr >= 0 else '不明')] += v
    g.close()
print('## 国教の法の分布（10 年ごと＝40 四半期ごと・種 1〜%d の国の数の合計）' % nseed)
for y in sorted({k[0] for k in dist}):
    print(y, ' '.join(f'{LV[l]}:{dist[(y,l)]}' for l in range(4) if dist[(y, l)]), '｜移民制限法', ' '.join(f'{IM[l]}:{imd[(y,l)]}' for l in range(3) if imd[(y, l)]))
print('## 種 1 の主な国（国教の法・移民制限法の段）')
for (n, y), v in sorted(us.items()):
    print(n, y, LV[v[0]], IM[v[1]])
T = sum(cls.values())
print('## 流れの分類（量の割合・行き先の国のその四半期の国教の法）')
for (k, l), v in sorted(cls.items(), key=lambda kv: -kv[1]):
    print(f'{k}\t{l}\t{v/T:.1%}')
