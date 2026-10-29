"""(651) 頻度の監視と注視の一覧の読み手。python an_mon.py <計器の出力ディレクトリ> [--seeds 1-10] [--tag m645] [--vs <別の出力> ...] [--items 1,2,...] [--repo <リポジトリ>]
  入力＝標準の計器（`INS=1 bash batch.sh run <腕>` の <出力>/ins）の表。読むだけ（C# を足さない）。出力＝標準出力へ（LF・UTF-8）。
  事前登録＝measurements/mon651_20261030_prereg.txt（追記 2 を含む）。閾値・単位・規則は全て AI の推奨（作者の承認は未確認）。判定ではない＝NG の集合 ng645 を動かさない。
  項目＝①使われない法 ②材の極端な不足と余り ③POP の偏り ④発見の偏り ⑤国庫の貯め込みと国債 ⑥政治の凍結 ⑦AI が一度も選ばない行動 ⑧価格の張り付き ⑨発散の記録 ⑩ファシズムの最初の年。
  出力の行：MONROW\t項目\t鍵\tn\t区分\t詳細／MONSUM\t項目\t…／MONSEED\t…／LIMIT\t…／UNREAD\t…。順序は鍵の辞書順（決定的）。
  環境変数 MON_MUTATE=<名前,…> は自己試験（selftest.py）の突然変異の入口（読み手の穴の確認用・普段は使わない）。"""
import sys, os, re, math, argparse, collections, statistics
from concurrent.futures import ProcessPoolExecutor

MUT = set(x for x in os.environ.get('MON_MUTATE', '').split(',') if x)
NAN = float('nan')

# ---------------------------------------------------------------- 規則の定数（事前登録 [2][3] と 追記 2） ----------------------------------------------------------------
CHRONIC_YEARS = 1 if 'chronic_years' in MUT else 10   # 慢性＝極端な年が 10 年以上かつ生きた年の 25% 以上
CHRONIC_SHARE = 0.25
SHORT_TH, STOCK_MONTHS_TH = 0.5, 24.0
PXR_HI, PXR_LO = 20.0, 0.05
PXR_HI_B1, PXR_LO_B1 = 50.0, 0.02
FLOOR_REL = 0.0 if 'floor_rel' in MUT else 0.01        # 追記 2 ③：流量・需要の相対の床＝国×財の活きた行の中央値の 1%
GENERAL_NATIONS = 3                                     # 財の一般の鍵＝その財で慢性の国が 3 国以上
PA_SINGLE, PA_FARMER = 0.5, 0.97
PB_SOLDIER = 0.10
PC_MIN_SHARE, PC_DROP, PC_YEARS = 0.005, (0.5 if 'pc_drop' in MUT else 0.1), 10
D2_NATIONS = 1 if 'd2_one' in MUT else 8
D3_SHARE = 0.0 if 'd3_zero' in MUT else 0.30
H1_TH, H3_TH = 1.0, 3.0
H2_YEARS = 1 if 'h2_one' in MUT else 40
FREEZE_YEARS = 1 if 'freeze_one' in MUT else 40
DIVERGE = 1000.0
WINDOW_Q = 0 if 'window_zero' in MUT else 12
DEFEAT_TH, EPI_TH, COLLAPSE_RATIO = 0.2, 0.01, 0.5
B2_TH, B3_ROWS = 0.5, 12
NGM9_SEEDS = 5
NINE = ['清', 'ロシア', 'オスマン', 'フランス', 'イギリス', 'プロイセン', 'オーストリア', '日本', 'アメリカ']

# 段の法の名簿（Government.LevelLaws の写し＝2026-10-30・35 行）。読める 23 行だけここに写す。(名前, 最上段, 表, 列, 変換, 印)
#   変換：i＝整数／hours＝労働時間(時間数)→段(14=0,12=1,10=2,8=3＝短いほど上の段)／ban＝児童労働が合法(1)→禁止(1)／bit:N＝nationalizedMaskCode のビット
LAWS = [
    ('教育法', 3, 'scalars', 'educationLaw', 'i', ''),
    ('参政権', 3, 'scalars', 'suffrage', 'i', ''),
    ('女性の権利', 3, 'scalars', 'womenRightsCode', 'i', ''),
    ('通貨制度', 2, 'lawgate', 'goldLawCode', 'i', ''),
    ('検閲', 2, 'scalars', 'censorship', 'i', ''),
    ('労働組合', 2, 'sbu', 'law', 'i', ''),
    ('労働時間', 3, 'scalars', 'workHoursValue', 'hours', ''),
    ('児童労働', 1, 'scalars', 'childLaborLegal', 'ban', ''),
    ('土地改革', 2, 'reform', 'landReformLawCode', 'i', ''),
    ('農奴解放', 2, 'reform', 'serfLawCode', 'i', ''),
    ('社会保障', 3, 'scalars', 'socialSecurityLawCode', 'i', ''),
    ('徴兵', 2, 'nation', 'conscriptionLaw', 'i', ''),
    ('官僚制', 2, 'scalars', 'bureaucracy', 'i', ''),
    ('反トラスト', 2, 'scalars', 'antitrustLawCode', 'i', ''),
    ('最低賃金', 3, 'labor', 'lawCode', 'i', 'R1門の既定は開'),
    ('国有化:鉄道', 1, 'scalars', 'nationalizedMaskCode', 'bit:1', ''),
    ('国有化:鉱山', 1, 'scalars', 'nationalizedMaskCode', 'bit:2', ''),
    ('国有化:軍需', 1, 'scalars', 'nationalizedMaskCode', 'bit:4', ''),
    ('国有化:石油', 1, 'scalars', 'nationalizedMaskCode', 'bit:16', ''),
    ('国有化:電力', 1, 'scalars', 'nationalizedMaskCode', 'bit:32', ''),
    ('国有化:全て', 1, 'scalars', 'nationalizedMaskCode', 'bit:64', 'R2政体で縛られる最上段'),
    ('植民地参政権', 2, 'sf', 'colonialSuffrageLaw', 'i', ''),
    ('移民原住民参政権', 1, 'sf', 'migrantNativeSuffrageLaw', 'i', ''),
]
UNREAD_LAWS = ['資本移動', '穀物法', '秘密投票', '移民制限', '技術者流出禁止', '国教', '同業組合(現行の段)', '戦時配給', '軍需輸出', '民間の武器購入', '阿片の保有', '阿片の輸出']
LAW_TABLE_COLS = {}
for _n, _t, _tb, _c, _f, _m in LAWS:
    LAW_TABLE_COLS.setdefault(_tb, [])
    if _c not in LAW_TABLE_COLS[_tb]: LAW_TABLE_COLS[_tb].append(_c)

LIMITS = [
    ('①', '年内に上げて戻した制定は年末の断面では見えない(9 国の四半期の列で件数だけ別に出す)／段の序数の向きは法で違う(労働時間は短いほど上の段・児童労働は禁止が 1)＝本表で向きを揃えた/読めない法は UNREAD に列挙(第 2 段の C# は先送り)/国名は表示名で建国・改称で別の名前になる'),
    ('②', '標本(pxgoods は 9 国×31 財×約 68 時点・goods_s は残存国の飛び飛びの年)で数える＝全 130 財・全ての国は読めない/年内の季節は見えない/輸送で届かない不足と生産が無い不足は区別しない/'
          '流量と需要の床は国×財の活きた行の中央値の 1%(AI の置き)/財は名前で引く(番号を書かない)'),
    ('③', '交戦中の兵士の膨張は見逃す設計(Pb は交戦中でない年末だけ)/階級の分類は計器の階級番号(失業のプールは階級として出ない)/Pa は農民以外の 1 階級の割合が 0.5 以上または農民が 0.97 以上で読んだ'),
    ('④', 'E の行は解禁であって使われたではない/伝わり(transmit)による連鎖の解禁は D2 に混ざる(transmitTerm の割合を併記)/engineer_s と disc_s に出ない row(全員が開幕に既知)は見えない'),
    ('⑤', '年末の断面(年内の借り換えは見えない)/国庫と GDP の名目は自国通貨(通貨単位の違う国どうしは比較しない)/分母(名目 GDP)が自分の中央値の 1% 未満の年は対象外にして別に数える'),
    ('⑥', '読める 23 行と政体の変化だけを数える＝読めない 12 行の変化を取りこぼし凍結を過大に出す(上限)/長い凍結が史実どおり(清の 19 世紀前半など)かは判定しない'),
    ('⑦', '結果の動詞は AI の選びそのものではない(議会・内戦・外圧でも起きる)/外交の動詞(同盟・講和・調停・列強の介入)・AI の関数の内側の分岐・建設の細目は拾えない/NationPolicy を通らない決定は見えない/プレイヤー国は無い(自己対戦)'),
    ('⑧', '範囲の端は軟らかい飽和(端に漸近)＝端に座ると端の 1/2 まで来るを区別しない/州の中央値で数える＝一部の州だけが端に座る場合は拾えない/B1 は②の極端に包含される(②の詳細に併記)'),
    ('⑨', '窓は直前 12 四半期の AI の推奨(作者の決めは直前の数年)/事象の閾値は AI の置き/開幕を低い値で取った国は比が大きく出る(分母の開幕の値を併記)/封鎖は拾えない(World.LastBlockadeRisk の書き手が無い＝常に 0・MultiNationSim.cs:1492)/'
          'NGM9 は記録だけ(止める線の効力なし)/国名は表示名で改称・建国は別の国になる'),
    ('⑩', '定義は idn_s の knownFas が最初に 1 になる四半期/史実との並べは記録だけ(採否に使わない)'),
    ('共通', '全ての国を年末の断面(四半期の番号 mod 4 が 3)に揃えて読む(9 国だけ毎四半期の表を使うと解像度の差が頻度の差に化ける)/10 種の頻度は種の運の影響を受ける(n(u) が基線・N1・N2 で 2 以上ばらつく鍵は 揺れる)/第 2 段の列(monlaw_s・mongood_s・monai_s)は先送り'),
]

# ---------------------------------------------------------------- 入出力 ----------------------------------------------------------------
def fl(s):
    try: return float(s)
    except Exception: return NAN

def isnum(x): return x == x and x not in (float('inf'), float('-inf'))

def stream(path, cols, miss=None):
    """行ごとに流す（全体を載せない）。表が無ければ None。列が無ければ '' を返し miss へ記録。"""
    if not os.path.isfile(path): return None
    return _stream(path, cols, miss)

def _stream(path, cols, miss):
    with open(path, encoding='utf-8', newline='') as f:
        h = f.readline().rstrip('\r\n').split('\t')
        ix = []
        for c in cols:
            if c in h: ix.append(h.index(c))
            else:
                ix.append(-1)
                if miss is not None: miss.add(os.path.basename(path).rsplit('_s', 1)[0] + '.' + c)
        for line in f:
            p = line.rstrip('\r\n').split('\t')
            yield tuple(p[i] if 0 <= i < len(p) else '' for i in ix)

def qint(s):
    try: return int(float(s))
    except Exception: return None

def tq(s):
    """class_s・stock_s・pxgoods_s の time('q123'/'w5'/'open'/'warm')→四半期の番号(q の行だけ)。"""
    if s[:1] == 'q':
        try: return int(s[1:])
        except Exception: return None
    return None

def is_ye(q):
    if 'ye_off' in MUT: return q is not None
    return q is not None and q % 4 == 3

def lawlevel(tf, v):
    x = fl(v)
    if not isnum(x): return None
    if tf == 'i': return int(round(x))
    if tf == 'hours': return 0 if x >= 13 else 1 if x >= 11 else 2 if x >= 9 else 3
    if tf == 'ban': return 1 if x < 0.5 else 0
    if tf.startswith('bit:'): return 1 if (int(round(x)) & int(tf[4:])) != 0 else 0
    return None

# ---------------------------------------------------------------- 1 種の読み ----------------------------------------------------------------

def general_keys(ext, items):
    """国を先頭にした鍵 国|型[|階級] を、その型で極端な国が GENERAL_NATIONS 国以上なら *|型[|階級] の一般の鍵に畳む（事前登録 [2]：特定の鍵と一般の鍵の 2 段）。"""
    for it in items:
        g = collections.defaultdict(set)
        for k in list(ext[it]):
            if k.startswith('*|'): continue
            parts = k.split('|', 1)
            if len(parts) == 2: g[parts[1]].add(parts[0])
        if 'key_one' in MUT: continue
        for typ, ns in g.items():
            if len(ns) >= GENERAL_NATIONS: ext[it]['*|' + typ] = '%d国' % len(ns)

def an_seed(args):
    d, s, pd_code = args
    P = lambda n: os.path.join(d, '%s_s%d.tsv' % (n, s))
    miss = set(); absent = set()
    R = {'seed': s, 'ext': collections.defaultdict(dict), 'uni': collections.defaultdict(set), 'cnt': {}, 'stat': collections.defaultdict(float), 'fasc': None, 'div': {}, 'nat': {}}
    def need(n):
        if not os.path.isfile(P(n)): absent.add(n); return False
        return True
    # ---- nation_s（全ての国×毎四半期）
    NS = collections.defaultdict(list)
    if need('nation'):
        for r in stream(P('nation'), ['quarter', 'year', 'nation', 'populationK', 'nominalGdpPerYear', 'civilWarMonths', 'conscriptionLaw', 'mobilizedFlag', 'coreRegions', 'integratedRegions',
                                       'ledgerBorrowCumulative', 'ledgerDevalueCumulative', 'sovereignDefaultCumulativeCount', 'monopolyPushHitCount', 'priceLevel'], miss):
            q = qint(r[0])
            if q is None: continue
            NS[r[2]].append((q, qint(r[1]), fl(r[3]), fl(r[4]), fl(r[5]), r[6], fl(r[7]), fl(r[8]) + fl(r[9]), fl(r[10]), fl(r[11]), fl(r[12]), fl(r[13]), fl(r[14])))
    R['nat'] = {n: len(v) for n, v in NS.items()}
    # ---- ① 使われない法：年末の断面で国ごとの段の系列
    LV = collections.defaultdict(lambda: collections.defaultdict(dict))   # law -> nation -> {q: level}
    QLV = collections.defaultdict(lambda: collections.defaultdict(dict))  # 9 国の四半期の系列（年内の往復用）
    POLITY = collections.defaultdict(dict)                                # nation -> {q: polityCode}
    LEGIT = collections.defaultdict(dict)
    DEFEAT = collections.defaultdict(dict)                                # nation -> {q: defeatShock}
    pd_seen = False
    tcols = {'scalars': ['quarter', 'nation', 'polityCode', 'defeatShock'], 'lawgate': ['quarter', 'nation'], 'sbu': ['quarter', 'nation'], 'reform': ['quarter', 'nation', 'legitimacyShare'],
             'nation': None, 'labor': ['quarter', 'nation'], 'sf': ['quarter', 'nation']}
    for tb, cl in LAW_TABLE_COLS.items():
        if tb == 'nation':
            # nation_s は上で読んだ NS の徴兵法(列 6)を使う
            for nname, rows in NS.items():
                for row in rows:
                    if is_ye(row[0]): LV['徴兵'][nname][row[0]] = lawlevel('i', row[5])
            continue
        if not need(tb): continue
        base = tcols[tb]
        laws_here = [(n, tf, c) for (n, t, tb2, c, tf, m) in LAWS if tb2 == tb]
        for r in stream(P(tb), base + cl, miss):
            q = qint(r[0])
            if q is None: continue
            nm = r[1]
            ye = is_ye(q)
            if tb == 'scalars':
                code = qint(r[2])
                if ye:
                    POLITY[nm][q] = code
                    if code == pd_code: pd_seen = True
                DEFEAT[nm][q] = fl(r[3])
            if tb == 'reform' and ye: LEGIT[nm][q] = fl(r[2])
            off = len(base)
            for (n, tf, c) in laws_here:
                v = r[off + cl.index(c)]
                lv = lawlevel(tf, v)
                if lv is None: continue
                if ye: LV[n][nm][q] = lv
                if tb == 'scalars' and nm in NINE: QLV[n][nm][q] = lv
    R['stat']['pd_seen'] = 1 if pd_seen else 0
    rises = {}; falls = {}
    for (n, top, tb, c, tf, m) in LAWS:
        if tb == 'nation': pass
        rs = [0] * (top + 1); fs = [0] * (top + 1)
        for nm, ser in LV[n].items():
            qs = sorted(ser)
            for a, b in zip(qs, qs[1:]):
                la, lb = ser[a], ser[b]
                if b - a != 4: continue
                for l in range(la + 1, lb + 1):
                    if 0 < l <= top: rs[l] += 1
                for l in range(lb + 1, la + 1):
                    if 0 < l <= top: fs[l] += 1
        rises[n] = rs; falls[n] = fs
        have = any(LV[n][nm] for nm in LV[n])
        if not have: continue
        r2 = ('R2' in m) and 'r2_off' not in MUT
        if 'r1_off' in MUT: m = ''
        for l in range(1, top + 1):
            key = '%s|段%d' % (n, l)
            R['uni']['1'].add(key)
            if r2 and not pd_seen:
                R['stat']['cond_off:' + key] = 1; continue
            if (rs[l] > 0 if 'law_inv' not in MUT else rs[l] == 0): continue
            R['ext']['1'][key] = '制定0/撤回%d%s' % (fs[l], ('|' + m) if m else '')
        key = '%s|全段' % n
        R['uni']['1'].add(key)
        if (sum(rs) == 0) and not (r2 and not pd_seen):
            R['ext']['1'][key] = '死んだ軸%s' % (('|' + m) if m else '')
    # 年内の往復（9 国の四半期の列・scalars 由来の法だけ）
    rt = 0
    for (n, top, tb, c, tf, m) in LAWS:
        if tb != 'scalars': continue
        for nm, ser in QLV[n].items():
            byyear = collections.defaultdict(list)
            for q in sorted(ser): byyear[q // 4].append((q, ser[q]))
            for y, lst in byyear.items():
                if len(lst) < 4: continue
                first, last = lst[0][1], lst[-1][1]
                prev = None
                for _, lv in lst:
                    if lv != first and lv != last and (lv > max(first, last) or lv < min(first, last)): rt += 1; break
    R['stat']['intra_year_roundtrip'] = rt
    # ---- ⑥ 政治の凍結：政体と読める法が年末の断面で 1 度も変わらない最長の連続
    allnat = set(POLITY)
    for n in LV: allnat |= set(LV[n])
    lawnames = [x[0] for x in LAWS]
    for nm in sorted(allnat):
        qs = sorted(set(POLITY[nm]) | set(q for n in lawnames for q in LV[n].get(nm, {})))
        best = 0; bs = None; run = 0; start = None; prevst = None; prevq = None
        for q in qs:
            st = (POLITY[nm].get(q),) + tuple(LV[n].get(nm, {}).get(q, -9) for n in lawnames)
            if prevst is not None and st == prevst and q - prevq == 4:
                run += 1
            else:
                run = 1; start = q
            if run > best: best = run; bs = start
            prevst = st; prevq = q
        if len(qs) >= 1:
            R['uni']['6'].add(nm)
        if best >= FREEZE_YEARS:
            lg = [LEGIT[nm][q] for q in LEGIT[nm] if bs is not None and bs <= q <= bs + 4 * (best - 1) and isnum(LEGIT[nm][q])]
            R['ext']['6'][nm] = '凍結%d年(%d〜)正統性平均%s' % (best, 1783 + (bs or 0) // 4, ('%.2f' % (sum(lg) / len(lg))) if lg else 'nan')
    # ---- ② 材（pxgoods_s と goods_s）／⑧ B1 を併記
    def goods_rows():
        if need('pxgoods'):
            for r in stream(P('pxgoods'), ['time', 'nation', 'goodName', 'lastProducedQty', 'lastDemandQty', 'lastShortageQty', 'lastImportQty', 'stockQty', 'priceOverRefTimesLevelMedianRatio'], miss):
                if tq(r[0]) is None: continue
                yield (r[1], r[2], fl(r[3]), fl(r[4]), fl(r[5]), fl(r[6]), fl(r[7]), fl(r[8]))
        if need('goods'):
            for r in stream(P('goods'), ['quarter', 'nation', 'goodName', 'lastProducedQty', 'lastDemandQty', 'lastShortageQty', 'lastImportQty', 'stockQty'], miss):
                yield (r[1], r[2], fl(r[3]), fl(r[4]), fl(r[5]), fl(r[6]), fl(r[7]), NAN)
    G = collections.defaultdict(list)
    for row in goods_rows(): G[(row[0], row[1])].append(row[2:])
    gen = collections.defaultdict(set)
    for (nm, gd), rows in sorted(G.items()):
        dem = sorted(x[1] for x in rows if isnum(x[1]) and x[1] > 0)
        flw = sorted((x[0] if isnum(x[0]) else 0) + (x[3] if isnum(x[3]) else 0) for x in rows)
        flwpos = [v for v in flw if v > 0]
        dfl = FLOOR_REL * (statistics.median(dem) if dem else 0)
        ffl = FLOOR_REL * (statistics.median(flwpos) if flwpos else 0)
        cnt = collections.Counter(); alive = 0; alive_px = 0; b1hi = b1lo = 0
        for (prod, dm, sh, im, stk, pxr) in rows:
            flow = (prod if isnum(prod) else 0) + (im if isnum(im) else 0)
            hasd = isnum(dm) and dm > 0 and dm >= dfl
            if hasd or flow >= max(ffl, 1e-300) and flow > 0: alive += 1
            if hasd and isnum(sh) and ((sh / dm >= SHORT_TH) if 'short_inv' not in MUT else (sh / dm < SHORT_TH)): cnt['short'] += 1
            if hasd and flow < max(ffl, 1e-300) and not (flow > 0 and ffl == 0 and 'floor_rel' in MUT):
                cnt['noProd'] += 1
            if flow > 0 and flow >= ffl and isnum(stk) and stk / flow >= STOCK_MONTHS_TH: cnt['glut'] += 1
            if 'floor_rel' in MUT and hasd and flow <= 1e-9 and isnum(stk) and stk > 0: cnt['glut'] += 1   # 突然変異：流量 0 でも在庫があれば余りに数える(分離を壊す)
            if isnum(pxr) and pxr > 0:
                alive_px += 1
                if pxr >= PXR_HI: cnt['pxHi'] += 1
                if pxr <= PXR_LO: cnt['pxLo'] += 1
                if pxr >= PXR_HI_B1: b1hi += 1
                if pxr <= PXR_LO_B1: b1lo += 1
        R['uni']['2'].add('%s|%s' % (nm, gd))
        for t in ('short', 'noProd', 'glut', 'pxHi', 'pxLo'):
            base = alive_px if t.startswith('px') else alive
            c = cnt[t]
            if base > 0 and c >= 6 and c / base >= CHRONIC_SHARE:
                extra = ''
                if t == 'pxHi': extra = '|B1(≥50)%d行' % b1hi
                if t == 'pxLo': extra = '|B1(≤0.02)%d行' % b1lo
                R['ext']['2']['%s|%s|%s' % (nm, gd, t)] = '%d/%d行%s' % (c, base, extra)
                gen[(gd, t)].add(nm)
    for (gd, t), ns in gen.items():
        if len(ns) >= GENERAL_NATIONS:
            R['ext']['2']['*|%s|%s' % (gd, t)] = '%d国' % len(ns)
    # ---- ③ POP の偏り
    NAMES = {}; CL = collections.defaultdict(lambda: collections.defaultdict(dict))   # nation -> q -> {classId: sizeK}
    if need('class'):
        for r in stream(P('class'), ['time', 'nation', 'classId', 'className', 'sizeK'], miss):
            q = tq(r[0])
            if not is_ye(q): continue
            NAMES[r[2]] = r[3]
            CL[r[1]][q][r[2]] = fl(r[4])
    FARMER, NOBLE = '1', '0'
    for nm in sorted(CL):
        qs = sorted(CL[nm])
        R['uni']['3'].add(nm)
        pa = 0; alive = 0
        peak = {}; ser = collections.defaultdict(list)
        for q in qs:
            sz = {k: v for k, v in CL[nm][q].items() if isnum(v) and v >= 0}
            tot = sum(sz.values())
            if tot <= 0: continue
            alive += 1
            f = sz.get(FARMER, 0) / tot
            nonf = max([v / tot for k, v in sz.items() if k != FARMER] or [0])
            if nonf >= PA_SINGLE or f >= PA_FARMER: pa += 1
            for k, v in sz.items():
                ser[k].append((q, v / tot))
        if alive and pa >= CHRONIC_YEARS and pa / alive >= CHRONIC_SHARE:
            R['ext']['3']['%s|Pa' % nm] = '%d/%d年' % (pa, alive)
        for k, lst in ser.items():
            if k in (FARMER, NOBLE): continue
            m = max(v for _, v in lst)
            if m < PC_MIN_SHARE: continue
            pk = [i for i, (_, v) in enumerate(lst) if v == m][0]
            best = run = 0; prev = None
            for q, v in lst[pk + 1:]:
                if v < m * PC_DROP and (prev is None or q - prev == 4): run += 1
                else: run = 1 if v < m * PC_DROP else 0
                prev = q if v < m * PC_DROP else None
                best = max(best, run)
            if best >= PC_YEARS:
                R['ext']['3']['%s|Pc|%s' % (nm, NAMES.get(k, k))] = '最高%.3f→1/10未満が%d年' % (m, best)
    SK = collections.defaultdict(dict)
    if need('stock'):
        for r in stream(P('stock'), ['time', 'nation', 'soldierToPopulationShare', 'atWarFlag', 'treasuryStock', 'govDebtStock'], miss):
            q = tq(r[0])
            if not is_ye(q): continue
            SK[r[1]][q] = (fl(r[2]), fl(r[3]), fl(r[4]), fl(r[5]))
    for nm in sorted(SK):
        rows = [(q, v) for q, v in SK[nm].items() if isnum(v[0])]
        if not rows: continue
        c = sum(1 for q, v in rows if v[0] >= PB_SOLDIER and (v[1] == 0 if 'pb_war' not in MUT else True))
        if c >= CHRONIC_YEARS and c / len(rows) >= CHRONIC_SHARE:
            R['ext']['3']['%s|Pb' % nm] = '%d/%d年' % (c, len(rows))
    # ---- ④ 発見の偏り
    uni_rows = set(); E = []
    if need('engineer'):
        for r in stream(P('engineer'), ['kind', 'row'], miss): uni_rows.add(r[1])
    if need('disc'):
        for r in stream(P('disc'), ['kind', 'quarter', 'nation', 'row', 'transmitTerm'], miss):
            uni_rows.add(r[3])
            if r[0] == 'E':
                q = qint(r[1])
                if q is not None: E.append((q, r[2], r[3], fl(r[4])))
    unlocked = collections.defaultdict(list)
    for q, nm, rw, tr in E: unlocked[rw].append((q, nm, tr))
    alive_q = collections.Counter()
    for nm, rows in NS.items():
        for row in rows:
            if row[2] > 0: alive_q[row[0]] += 1
    for rw in sorted(uni_rows):
        R['uni']['4'].add('D1|' + rw)
        if rw not in unlocked: R['ext']['4']['D1|' + rw] = '誰も解禁しない'
    bq = collections.defaultdict(lambda: collections.defaultdict(list))
    for rw, lst in unlocked.items():
        for q, nm, tr in lst: bq[rw][q].append(tr)
    for rw in sorted(bq):
        for q, trs in sorted(bq[rw].items()):
            if q == 0: continue
            nn = len(trs); half = alive_q.get(q, 0) / 2
            if nn >= D2_NATIONS or (nn >= 2 and nn >= half and half > 0):
                tr = sum(1 for t in trs if isnum(t) and t > 0) / nn
                R['ext']['4']['D2|' + rw] = '四半期%d・%d国(生存%d)・伝わり項>0の割合%.2f' % (q, nn, alive_q.get(q, 0), tr)
                break
    first = collections.Counter(); nrows = 0
    for rw, lst in unlocked.items():
        qs = [q for q, _, _ in lst if q > 0]
        if not qs: continue
        nrows += 1
        mq = min(qs)
        for nm in set(n for q, n, _ in lst if q == mq): first[nm] += 1
    for nm, c in sorted(first.items()):
        R['uni']['4'].add('D3|' + nm)
        if nrows and c / nrows >= D3_SHARE and c >= 1:
            R['ext']['4']['D3|' + nm] = '最初の解禁%d/%d行' % (c, nrows)
    # ---- ⑤ 国庫・国債
    gdp = collections.defaultdict(dict)
    for nm, rows in NS.items():
        for row in rows:
            if is_ye(row[0]): gdp[nm][row[0]] = row[3]
    for nm in sorted(SK):
        gs = [v for v in gdp.get(nm, {}).values() if isnum(v) and v > 0]
        if not gs: continue
        med = statistics.median(gs)
        thin = 0; h1 = h3 = alive = 0; zero = True; zn = 0
        for q, v in sorted(SK[nm].items()):
            g = gdp[nm].get(q, NAN)
            if not (isnum(g) and g > 0): continue
            if g < 0.01 * med: thin += 1; continue
            alive += 1
            tr, db = v[2], v[3]
            if isnum(tr) and tr / g >= H1_TH: h1 += 1
            if isnum(db) and db / g >= H3_TH: h3 += 1
            if not (isnum(db) and db <= 1e-9 * g): zero = False
        R['uni']['5'].add(nm)
        R['stat']['denom_thin'] += thin
        if alive >= CHRONIC_YEARS and h1 >= CHRONIC_YEARS and h1 / alive >= CHRONIC_SHARE: R['ext']['5']['%s|H1' % nm] = '%d/%d年' % (h1, alive)
        if alive >= CHRONIC_YEARS and h3 >= CHRONIC_YEARS and h3 / alive >= CHRONIC_SHARE: R['ext']['5']['%s|H3' % nm] = '%d/%d年' % (h3, alive)
        if alive >= H2_YEARS and zero: R['ext']['5']['%s|H2' % nm] = '国債0が全期間%d年' % alive
    # ---- ⑧ B2（労働の値段）・B3（物価水準の旧天井／床の接触）
    if need('labor'):
        LB = collections.defaultdict(lambda: [0, 0, 0])
        for r in stream(P('labor'), ['quarter', 'nation', 'plainTinyPriceRegionShare', 'farmTinyPriceRegionShare'], miss):
            q = qint(r[0])
            if not is_ye(q): continue
            a = LB[r[1]]; a[0] += 1
            if (fl(r[2]) >= B2_TH) != ('b2_inv' in MUT): a[1] += 1
            if (fl(r[3]) >= B2_TH) != ('b2_inv' in MUT): a[2] += 1
        for nm in sorted(LB):
            n_, p_, f_ = LB[nm]
            R['uni']['8'].add(nm)
            if n_ and p_ >= CHRONIC_YEARS and p_ / n_ >= CHRONIC_SHARE: R['ext']['8']['%s|B2plain' % nm] = '%d/%d年' % (p_, n_)
            if n_ and f_ >= CHRONIC_YEARS and f_ / n_ >= CHRONIC_SHARE: R['ext']['8']['%s|B2farm' % nm] = '%d/%d年' % (f_, n_)
    if need('pclamp'):
        PC = collections.Counter()
        for r in stream(P('pclamp'), ['nation'], miss): PC[r[0]] += 1
        for nm, c in sorted(PC.items()):
            R['uni']['8'].add(nm)
            if c >= B3_ROWS: R['ext']['8']['%s|B3' % nm] = '%d行' % c
    general_keys(R['ext'], ('3', '5', '8'))
    # ---- ⑦ 動詞の回数（0 の判定は集計側＝種をまたいだ宇宙が要る）
    C = R['cnt']
    if NS:
        C['借入'] = max([row[8] for rows in NS.values() for row in rows if isnum(row[8])] or [0])
        C['切り下げ'] = max([row[9] for rows in NS.values() for row in rows if isnum(row[9])] or [0])
        C['不履行'] = max([row[10] for rows in NS.values() for row in rows if isnum(row[10])] or [0])
        C['独占の推し'] = max([row[11] for rows in NS.values() for row in rows if isnum(row[11])] or [0])
        C['総動員'] = sum(1 for rows in NS.values() for row in rows if row[6] == 1)
    if need('aib'):
        cats = collections.Counter()
        for r in stream(P('aib'), ['startedByCategory'], miss):
            for part in r[0].split(';'):
                if ':' in part:
                    k, v = part.rsplit(':', 1)
                    cats[k] += fl(v) if isnum(fl(v)) else 0
        for k, v in cats.items(): C['建設:' + k] = v
    if need('wars'):
        ext_w = civ_w = 0
        for r in stream(P('wars'), ['civilWarFlag'], miss):
            if r[0] == '1': civ_w += 1
            else: ext_w += 1
        C['戦争(外戦)'] = ext_w; C['戦争(内戦)'] = civ_w
    if need('invade'):
        C['侵略'] = sum(1 for r in stream(P('invade'), ['submitted'], miss) if r[0] == '1')
    if need('canal'):
        for r in stream(P('canal'), ['canalId', 'openedQuarter'], miss): C['運河:' + r[0]] = 1 if (qint(r[1]) or -1) >= 0 else 0
    # ---- ⑨ 発散
    EPI = collections.defaultdict(list)
    if need('epi'):
        for r in stream(P('epi'), ['quarter', 'nation', 'deadK'], miss):
            q = qint(r[0])
            if q is not None: EPI[r[1]].append((q, fl(r[2])))
    for nm in sorted(NS):
        rows = [r for r in NS[nm]]
        base = None
        for row in rows:
            if isnum(row[12]) and row[12] > 0: base = row[12]; break
        if base is None: continue
        above = [row for row in rows if isnum(row[12]) and row[12] / base >= DIVERGE]
        R['uni']['9'].add(nm)
        if not above: continue
        qc = above[0][0]; mx = max(r[12] for r in above) / base
        lo, hi = qc - WINDOW_Q, qc
        ev = []
        if 'ev_defeat' not in MUT:
            for q, v in DEFEAT.get(nm, {}).items():
                if lo <= q <= hi and isnum(v) and v >= DEFEAT_TH: ev.append('敗戦'); break
        if 'ev_civil' not in MUT:
            if any(lo <= row[0] <= hi and isnum(row[4]) and row[4] > 0 for row in rows): ev.append('内戦')
        if 'ev_epi' not in MUT:
            pop = next((row[2] for row in rows if row[0] >= qc and isnum(row[2]) and row[2] > 0), NAN)
            dead = sum(v for q, v in EPI.get(nm, []) if lo <= q <= hi and isnum(v))
            if isnum(pop) and pop > 0 and dead / pop >= EPI_TH: ev.append('疫病')
        if 'ev_collapse' not in MUT:
            reg = [(row[0], row[7]) for row in rows if lo <= row[0] <= hi and isnum(row[7])]
            if reg and reg[0][1] > 0 and min(v for _, v in reg) <= COLLAPSE_RATIO * reg[0][1]: ev.append('崩壊')
        last = rows[-1][0]
        gone = last < max(r[0] for rs_ in NS.values() for r in rs_) - 4
        R['div'][nm] = {'qc': qc, 'year': 1783 + qc // 4, 'ratio': mx, 'years': len(above) / 4.0, 'base': base, 'ev': ev, 'gone': gone}
    # ---- ⑩ ファシズム
    fq = None
    if need('idn'):
        for r in stream(P('idn'), ['quarter', 'year', 'nation', 'knownFas'], miss):
            q = qint(r[0])
            if q is not None and r[3] == '1' and (fq is None or (q > fq[0] if 'fasc_last' in MUT else q < fq[0])): fq = (q, qint(r[1]), r[2])
    sq = None
    if need('ideology'):
        for r in stream(P('ideology'), ['quarter', 'year', 'nation', 'ideoNationalFascismShare'], miss):
            q = qint(r[0])
            if q is not None and fl(r[3]) >= 0.10 and (sq is None or q < sq[0]): sq = (q, qint(r[1]), r[2])
    R['fasc'] = {'known': fq, 'share': sq}
    R['stat']['miss'] = len(miss)
    R['miss'] = sorted(miss); R['absent'] = sorted(absent)
    return R

# ---------------------------------------------------------------- 集計と出力 ----------------------------------------------------------------
def classify(n, N):
    if n >= N and N > 0: return '先頭'
    if n >= (N + 1) // 2 and n >= 2: return '注視'
    if n >= 2: return '記録'
    return '想定内'

def parse_seeds(s):
    if ',' in s: return [int(x) for x in s.split(',')]
    a, b = (s.split('-') + [s])[:2] if '-' in s else (s, s)
    return list(range(int(a), int(b) + 1))

def polity_pd(repo):
    p = os.path.join(repo, 'PriceOfBread.SimCore', 'Model.cs')
    try:
        txt = open(p, encoding='utf-8').read()
        m = re.search(r'public enum Polity\s*\{(.*?)\}', txt, re.S)
        names = [re.match(r'\s*(\w+)', l).group(1) for l in m.group(1).split('\n') if re.match(r'\s*\w+', l) and not l.strip().startswith('//')]
        return names.index('ProletarianDictatorship')
    except Exception:
        return 6

def lawtable_rows(repo):
    p = os.path.join(repo, 'PriceOfBread.SimCore', 'Government.cs')
    try:
        txt = open(p, encoding='utf-8').read()
        i = txt.index('LevelLaws =\n'); j = txt.index('];', i)
        return len(re.findall(r'^\s*new\(PolicyAxis\.', txt[i:j], re.M))
    except Exception:
        return -1

def analyze(d, seeds, pd_code, workers):
    jobs = [(d, s, pd_code) for s in seeds if os.path.isfile(os.path.join(d, 'nation_s%d.tsv' % s)) or os.path.isfile(os.path.join(d, 'scalars_s%d.tsv' % s))]
    if workers > 1 and len(jobs) > 1:
        with ProcessPoolExecutor(max_workers=min(workers, len(jobs))) as ex: res = list(ex.map(an_seed, jobs))
    else: res = [an_seed(j) for j in jobs]
    res.sort(key=lambda r: r['seed'])
    return res

def aggregate(res):
    N = len(res)
    agg = {}
    for it in '12345689':
        keys = collections.defaultdict(list)
        for r in res:
            for k, det in r['ext'][it].items(): keys[k].append((r['seed'], det))
        agg[it] = keys
    # ⑦
    univ = set()
    for r in res: univ |= set(r['cnt'])
    k7 = collections.defaultdict(list)
    for r in res:
        for v in univ:
            c = r['cnt'].get(v, 0)
            if (c == 0) if 'verb_one' not in MUT else (c <= 1): k7[v].append((r['seed'], '0回'))
    agg['7'] = k7
    # ⑨：事象の無い発散＝先頭・事象あり＝見逃す
    k9 = collections.defaultdict(lambda: {'free': [], 'seen': []})
    for r in res:
        for nm, v in r['div'].items():
            (k9[nm]['seen'] if v['ev'] else k9[nm]['free']).append((r['seed'], v))
    agg['9'] = k9
    return N, agg

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('dir'); ap.add_argument('--seeds', default='1-10'); ap.add_argument('--tag', default='')
    ap.add_argument('--vs', nargs='*', default=[]); ap.add_argument('--items', default='1,2,3,4,5,6,7,8,9,10')
    ap.add_argument('--repo', default=os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', '..'))
    ap.add_argument('--workers', type=int, default=min(10, os.cpu_count() or 1))
    a = ap.parse_args()
    items = set(a.items.split(','))
    seeds = sorted(set(parse_seeds(a.seeds)))   # 種の順に依らない（C4）
    repo = os.path.abspath(a.repo)
    pd_code = polity_pd(repo)
    out = []
    W = out.append
    W('MONHEAD\t読み手 an_mon.py (651)・タグ=%s・入力=%s・種=%s' % (a.tag, os.path.basename(os.path.dirname(os.path.abspath(a.dir))) + '/' + os.path.basename(os.path.abspath(a.dir)), ','.join(str(x) for x in seeds)))
    lt = lawtable_rows(repo)
    W('MONHEAD\t段の法の名簿：読める %d 行・読めない %d 行・合計 %d(Government.LevelLaws は %s 行)%s' % (len(LAWS), len(UNREAD_LAWS), len(LAWS) + len(UNREAD_LAWS), lt if lt >= 0 else '不明',
        '' if lt in (-1, len(LAWS) + len(UNREAD_LAWS)) else '  ⚠名簿の写しが古い'))
    W('MONHEAD\tProletarianDictatorship の polityCode=%d(Model.cs の enum Polity から実行時に引く)' % pd_code)
    res = analyze(a.dir, seeds, pd_code, a.workers)
    N, agg = aggregate(res)
    others = []
    for vd in a.vs:
        r2 = analyze(vd, seeds, pd_code, a.workers)
        others.append((os.path.basename(os.path.abspath(os.path.dirname(vd))) + '/' + os.path.basename(os.path.abspath(vd)), r2, aggregate(r2)))
    W('MONHEAD\t種の数 N=%d(%s)' % (N, ','.join(str(r['seed']) for r in res)))
    nm_names = {'1': '使われない法', '2': '材の極端', '3': 'POPの偏り', '4': '発見の偏り', '5': '国庫・国債', '6': '政治の凍結', '7': 'AIが選ばない行動', '8': '価格の張り付き', '9': '発散', '10': 'ファシズム'}
    absent_all = sorted(set(x for r in res for x in r['absent'])); miss_all = sorted(set(x for r in res for x in r['miss']))
    for t in absent_all: W('MONHEAD\t表なし：%s' % t)
    for t in miss_all: W('MONHEAD\t列なし：%s' % t)
    for r in res:
        W('MONSEED\t種%d\t国数=%d\t政体PDあり=%d\t年内往復=%d\t分母が痩せた年=%d' % (r['seed'], len(r['nat']), r['stat']['pd_seen'], r['stat']['intra_year_roundtrip'], r['stat']['denom_thin']))
    def vsn(it, key):
        parts = []; ns = []
        for lab, r2, (N2, ag2) in others:
            if it == '9': n = len(ag2[it].get(key, {'free': []})['free']) if key in ag2[it] else 0
            else: n = len(ag2[it].get(key, []))
            ns.append(n); parts.append('%s=%d' % (lab.split('/')[-1], n))
        return parts, ns
    def emit(it, key, n, cls, detail, ns_base):
        vs, ns = vsn(it, key)
        allns = [ns_base] + ns
        shake = (max(allns) - min(allns) >= 2) if ns else False
        extra = (' |vs ' + ' '.join(vs)) if vs else ''
        if shake: extra += ' |揺れる'
        W('MONROW\t%s\t%s\t%d\t%s\t%s%s' % (it, key, n, cls, detail, extra))
    for it in '12345678':
        if it not in items: continue
        keys = agg[it]; counts = collections.Counter()
        uni = set()
        for r in res: uni |= r['uni'][it]
        spec = [k for k in keys if not k.startswith('*|')]
        for k in sorted(keys):
            n = len(keys[k]); cls = classify(n, N); counts[cls] += 1
            if it == '2' and k.startswith('*|'):
                pass
            if cls == '想定内': continue
            dets = '; '.join('s%d:%s' % (sd, dd) for sd, dd in keys[k][:3]) + ('…' if len(keys[k]) > 3 else '')
            emit(it, k, n, cls, dets, n)
        if it == '7':
            usz = len(univ_of(res))
        else: usz = len(uni)
        top = sum(1 for k in spec if len(keys[k]) >= (N + 1) // 2 and len(keys[k]) >= 2)
        sat = ' 飽和' if usz and top / usz > 0.30 else ''
        W('MONSUM\t%s(%s)\t鍵の宇宙=%d\t先頭=%d\t注視=%d\t記録=%d\t想定内=%d%s' % (it, nm_names[it], usz, counts['先頭'], counts['注視'], counts['記録'], counts['想定内'], sat))
        if it == '1':
            W('MONSUM\t1(補足)\t条件なし(R2)=%d種分\t年内往復(9国×scalars法)=%s' % (sum(1 for r in res for k in r['stat'] if k.startswith('cond_off:')), ','.join('%d' % r['stat']['intra_year_roundtrip'] for r in res)))
        if it == '8': W('MONSUM\t8(補足)\tB1 は②の極端に包含される(pxr≥50 が慢性＝pxr≥20 も慢性)＝②の詳細に併記')
    if '9' in items:
        k9 = agg['9']; counts = collections.Counter(); tot_pairs = 0
        for nm in sorted(k9):
            fr, sn = k9[nm]['free'], k9[nm]['seen']
            tot_pairs += len(fr) + len(sn)
            if fr: cls = '先頭'
            else: cls = '見逃す'
            counts[cls] += 1
            def fmt(lst):
                return ','.join('s%d:%d年×%.0f(開幕%.3g,%.1f年)%s%s' % (sd, v['year'], v['ratio'], v['base'], v['years'], ('[' + '+'.join(v['ev']) + ']') if v['ev'] else '', '消滅後' if v['gone'] else '') for sd, v in lst[:4])
            det = '事象なし%d種:%s／事象あり%d種:%s' % (len(fr), fmt(fr), len(sn), fmt(sn))
            vs = ''
            if others:
                ps = []
                for lab, r2, (N2, ag2) in others:
                    x = ag2['9'].get(nm)
                    ps.append('%s=%d' % (lab.split('/')[-1], len(x['free']) if x else 0))
                vs = ' |vs ' + ' '.join(ps)
            W('MONROW\t9\t%s\t%d\t%s\t%s%s' % (nm, len(fr), cls, det, vs))
            if len(fr) >= NGM9_SEEDS: W('NGM9\t%s\t事象のない発散が%d種(記録だけ・止める線の効力なし・ng645 に混ぜない)' % (nm, len(fr)))
        W('MONSUM\t9(%s)\t(国,種)の組=%d\t先頭=%d\t見逃す=%d\t比が1000以上になった国=%d' % (nm_names['9'], tot_pairs, counts['先頭'], counts['見逃す'], len(k9)))
    if '10' in items:
        fk = [r['fasc']['known'] for r in res if r['fasc'] and r['fasc']['known']]
        sk = [r['fasc']['share'] for r in res if r['fasc'] and r['fasc']['share']]
        for r in res:
            f = r['fasc']
            W('MONROW\t10\t種%d\t1\t記録\t知識の最初=%s／割合≥0.10の最初=%s' % (r['seed'], ('%d年・%s' % (f['known'][1], f['known'][2])) if f['known'] else 'なし',
                                                                       ('%d年・%s' % (f['share'][1], f['share'][2])) if f['share'] else 'なし'))
        if fk:
            ys = sorted(x[1] for x in fk); cn = collections.Counter(x[2] for x in fk)
            W('MONSUM\t10(ファシズム)\t知識の最初の年 中央=%s 最小=%d 最大=%d\t国の度数=%s\t史実=伊1919〜22・独1920年代〜1933(並べるだけ)' % (statistics.median(ys), ys[0], ys[-1], ','.join('%s:%d' % kv for kv in sorted(cn.items()))))
        else: W('MONSUM\t10(ファシズム)\tknownFas が 1 になった種なし')
        if sk:
            ys = sorted(x[1] for x in sk)
            W('MONSUM\t10(補足)\t割合≥0.10の最初の年 中央=%s 最小=%d 最大=%d(n=%d)' % (statistics.median(ys), ys[0], ys[-1], len(ys)))
    W('MONSUM\t判定\tこの出力は注視の一覧の材料であって判定ではない(NG の集合 ng645 を動かさない)')
    for tag, txt in LIMITS: W('LIMIT\t%s\t%s' % (tag, txt))
    W('UNREAD\t第 1 段で読めない法 %d 行：%s' % (len(UNREAD_LAWS), '・'.join(UNREAD_LAWS)))
    W('UNREAD\t工業の財(鉄・石炭・鋼ほか約 100 財)は全ての国で読める表が無い(②は消費財 31 財の標本)')
    W('UNREAD\tAI の選びを数えた表が無い(⑦は結果の動詞だけ)・封鎖の列が無い(⑨)')
    sys.stdout.reconfigure(encoding='utf-8', newline='\n')
    sys.stdout.write('\n'.join(out) + '\n')

def univ_of(res):
    u = set()
    for r in res: u |= set(r['cnt'])
    return u

if __name__ == '__main__':
    main()
