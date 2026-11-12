# (1086) 読むだけ：越境の移民の流れ（xmig_s）と、行き先の州・国の居住者の文化と宗教の札（world.json）を並べる。
# 使い方：python -X utf8 -I flows.py <base_ins のフォルダ> <world.json> <種の数> > out
# 限界（自己申告）：
#  - 州の札は world.json の静的な 1 州 1 札（州の中の構成は持たない）。植民地格などで world.json に無い州は「不明」。
#  - xmig_s の量は受入の上限で縮めた後の flow（MigrateBetweenNations の f*scale → MoveMigrants → Instruments.CrossBorder）。
#    Pop.Blend の切り詰めの前＝実際に動いた量との差は MoveAbroad の中の Size の切り詰めだけ。縮める前の要求量（toX）と上限 Cap はどの表にも無い。
#  - 開幕の dstA（四半期 0・種 1）：民族統一度＝本国の州は札が主流と一致で 1・不一致で 0・空欄で 1（Simulation.cs:146-167）、
#    植民地格の州は 0（secession_s の四半期 0 の Colonial の行の core＝0.00 で確かめた）。人口は region_s の四半期 0。
#    識字は reform_s の四半期 0 の literacyShare（9 カ国・植民地を含む・兵士を除く）。無い国は world.json の literacy（本国だけ＝★印）。
#    途中の四半期の dstA は今の表からは作れない（民族統一度の列が無い）。
import sys, json, collections as C

base, wj, nseed = sys.argv[1], sys.argv[2], int(sys.argv[3])
W = json.load(open(wj, encoding='utf-8'))

def fam(r):
    return {'カトリック': '西方キリスト', 'プロテスタント': '西方キリスト', '正教': '東方キリスト', 'エチオピア正教': '東方キリスト',
            'イスラム': 'イスラム', 'シーア派': 'イスラム', 'ザイド派': 'イスラム', 'ヒンドゥー教': 'インド系', 'ヒンドゥー': 'インド系',
            'シク教': 'インド系', '上座部仏教': '仏教', 'チベット仏教': '仏教', '儒仏': '東アジア', '儒': '東アジア', '神仏': '東アジア',
            '土着信仰': '民俗'}.get(r, '他' if r else '')

reg = {}      # 州 -> (文化, 宗教, 国)
natpop = C.defaultdict(lambda: C.Counter())   # 国 -> 文化 -> 開幕の人口の割合
natrel = C.defaultdict(lambda: C.Counter())
lit0 = {}
for x in W['nations']:
    lit0[x['name']] = x.get('literacy', 0.0)
    for p in x['provinces']:
        cu, re = p.get('culture', ''), p.get('religion', '')
        reg[p['city']] = (cu, re, x['name'])
        natpop[x['name']][cu] += p.get('share', 0.0)
        natrel[x['name']][re] += p.get('share', 0.0)

# 開幕の dstA の近似
print('## 開幕の dstA の近似（四半期 0・種 1・EthnicDraw=0.7・DiscriminationLiteracy=0.45・★＝識字は本国だけの world.json）')
rp = C.defaultdict(list)   # 国 -> [(州, 格, 人口K)]
f = open(f'{base}/region_s1.tsv', encoding='utf-8')
h = f.readline().rstrip('\n').split('\t'); iq, inn, ir, it, ip = (h.index(k) for k in ('quarter', 'nation', 'region', 'tier', 'populationK'))
for line in f:
    c = line.rstrip('\n').split('\t')
    if int(c[iq]) != 0: break
    rp[c[inn]].append((c[ir], c[it], float(c[ip])))
f.close()
litq0 = {}
f = open(f'{base}/reform_s1.tsv', encoding='utf-8')
h = f.readline().rstrip('\n').split('\t'); iq, inn, il = h.index('quarter'), h.index('nation'), h.index('literacyShare')
for line in f:
    c = line.rstrip('\n').split('\t')
    if int(c[iq]) == 0: litq0[c[inn]] = float(c[il])
f.close()
dsta = {}
for nm, rows in rp.items():
    byc = C.Counter()
    for r, t, p in rows:
        if t != 'Colonial' and r in reg and reg[r][0]: byc[reg[r][0]] += p
    main = max(byc.items(), key=lambda kv: kv[1])[0] if byc else ''
    s = cpop = 0.0
    for r, t, p in rows:
        if t == 'Colonial': core = 0.0; cpop += p
        elif r in reg and reg[r][0] and main: core = 1.0 if reg[r][0] == main else 0.0
        else: core = 1.0
        s += p * core
    P = sum(p for _, _, p in rows)
    unity = s / P if P > 0 else 1.0
    l = litq0.get(nm, lit0.get(nm, 0.0)); star = '' if nm in litq0 else '★'
    dsta[nm] = ((1 + 0.7 * (1 - unity)) * min(1.0, max(0.0, l / 0.45)), 1 + 0.7 * (1 - unity), min(1.0, l / 0.45), unity, l, star, cpop / P if P > 0 else 0)
for nm, (a, fe, fl, u, l, star, cs) in sorted(dsta.items(), key=lambda kv: -kv[1][0]):
    print(f'{nm}\t dstA={a:.3f}\t 民族の因子={fe:.2f}\t 識字の因子={fl:.2f}{star}\t unity={u:.2f}\t lit={l:.2f}\t 植民地の人口の割合={cs:.2f}')

tot = [0.0] * (nseed + 1)
by_dec = C.Counter(); by_dst = C.Counter(); by_src = C.Counter(); corr = C.Counter()
m_cu = C.Counter(); m_re = C.Counter(); m_fam = C.Counter()
natshare_cu = 0.0; natshare_re = 0.0; known = 0.0
for s in range(1, nseed + 1):
    f = open(f'{base}/xmig_s{s}.tsv', encoding='utf-8')
    h = f.readline().rstrip('\n').split('\t')
    iq, ifr, ito, ifn, itn, iv = (h.index(k) for k in ('quarter', 'fromRegion', 'toRegion', 'fromNation', 'toNation', 'movedKPerQuarter'))
    for line in f:
        c = line.rstrip('\n').split('\t')
        v = float(c[iv])
        if not (v > 0):
            continue
        q = int(c[iq]); fr, to, fn, tn = c[ifr], c[ito], c[ifn], c[itn]
        tot[s] += v
        by_dec[1783 + q // 4 // 10 * 10] += v
        by_dst[tn] += v; by_src[fn] += v; corr[(fn, tn)] += v
        a, b = reg.get(fr), reg.get(to)
        if a is None or b is None or not a[0] or not b[0]:
            m_cu['不明'] += v
        else:
            m_cu['同じ' if a[0] == b[0] else '違う'] += v
        if a is None or b is None or not a[1] or not b[1]:
            m_re['不明'] += v; m_fam['不明'] += v
        else:
            m_re['同じ' if a[1] == b[1] else '違う'] += v
            m_fam['同じ' if fam(a[1]) == fam(b[1]) else '違う'] += v
        # 行き先の国の居住者（開幕の割合）のうち出る州と同じ文化・宗教の割合（距離 0/1 の人口加重の平均＝1−これ）
        if a is not None and a[0] and tn in natpop:
            t = sum(natpop[tn].values())
            natshare_cu += v * (natpop[tn][a[0]] / t if t > 0 else 0)
            natshare_re += v * (natrel[tn][a[1]] / t if t > 0 and a[1] else 0)
            known += v
    f.close()
T = sum(tot)
print(f'\n## 越境の流れ（xmig_s・種 1〜{nseed}）：合計 {T/nseed:.0f}K/種（種ごと ' + ' '.join(f'{x:.0f}' for x in tot[1:]) + '）')
print('10 年ごと（K/種）：' + ' '.join(f'{k}:{v/nseed:.0f}' for k, v in sorted(by_dec.items())))
print('行き先の国の上位：' + ' '.join(f'{k}:{v/T:.1%}' for k, v in by_dst.most_common(10)))
print('出る国の上位：' + ' '.join(f'{k}:{v/T:.1%}' for k, v in by_src.most_common(10)))
print('組の上位：' + ' '.join(f'{a}→{b}:{v/T:.1%}' for (a, b), v in corr.most_common(12)))
print('州の札の一致（量の割合）：文化 ' + ' '.join(f'{k}:{v/T:.1%}' for k, v in m_cu.items())
      + '｜宗教 ' + ' '.join(f'{k}:{v/T:.1%}' for k, v in m_re.items())
      + '｜宗教の族 ' + ' '.join(f'{k}:{v/T:.1%}' for k, v in m_fam.items()))
if known > 0:
    print(f'行き先の国の開幕の居住者のうち出る州と同じ文化の割合（量で加重）：{natshare_cu/known:.1%}・同じ宗教：{natshare_re/known:.1%}（札の分かる量 {known/T:.1%}）')
