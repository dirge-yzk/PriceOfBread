"""空洞化の読み手（an_h2.py・edge_689.py）の共通部品（(718) hol2・(722) hol3）：places.json と world.json の読み込み・距離・**例外の名簿の2つの版**。
  -ex rule ＝既定（(722) hol3 で既定にした＝止める線の例外の規則。★(719) ① 作者「いったん全て推奨で」で推奨を採った・効くのは波51 から）。
             places.json が植民地格（kind＝colony）で、緯度の絶対値が RULE_LAT 度以上で、**開幕の人口が RULE_OPEN_K 千人未満**の州。
             **開幕の人口は world.json の popK**（名前と popK を持つ項目）で判定する＝腕の出力の最初の四半期を読まない（(722)・腕によって名簿が変わらない）。
             world.json に popK が無い州は例外にしない（NaN は < で偽）。
             州の名前は書かない・本国格や州（kind＝state）は絶対に例外にならない・主要国の州は例外にしない。
  -ex old  ＝旧の名簿。名前の名簿 EXEMPT_OLD（ヌーク・ナイン・イカルイト・フォートシンプソン）＝(368)(584) の版。出力は (718) の前の既定とバイトで同じ。"""
import os, json, math
PL = {}      # 名前 → (緯度, 経度)
KIND = {}    # 名前 → places.json の kind（state・colony・target）
# ★(144)：リポジトリの places.json を先に読む（最初に見つかった1枚）。2026-10-10 リポジトリの相対を先頭へ（scratchpad 時代の絶対パスは後ろ）
for cand in [os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', '..', 'PriceOfBread.SimCore', 'places.json'), 'C:/Users/dirge/Documents/PriceOfBread/PriceOfBread.SimCore/places.json']:
    if os.path.exists(cand):
        for p in json.load(open(cand, encoding='utf-8')):
            PL[p['name']] = (p['lat'], p['lon']); KIND[p['name']] = p.get('kind', '')
        break
if not PL: print('⚠ places.json が見つからない＝距離の列は nan')
POPK = {}    # (722) 名前 → world.json の popK（開幕の人口・千人）。名前と popK を持つ項目を全て拾う（initialColonies・abstractTargets の colonies ほか）
def _walk_popk(o):
    if isinstance(o, dict):
        if 'name' in o and 'popK' in o: POPK[o['name']] = float(o['popK'])
        for v in o.values(): _walk_popk(v)
    elif isinstance(o, list):
        for v in o: _walk_popk(v)
for cand in [os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', '..', 'PriceOfBread.SimCore', 'world.json'), 'C:/Users/dirge/Documents/PriceOfBread/PriceOfBread.SimCore/world.json']:
    if os.path.exists(cand):
        _walk_popk(json.load(open(cand, encoding='utf-8')))
        break
if not POPK: print('⚠ world.json が見つからない＝規則の版の例外は 0 件')
def dist(a, b):
    if a not in PL or b not in PL: return float('nan')
    la1, lo1 = map(math.radians, PL[a]); la2, lo2 = map(math.radians, PL[b])
    h = math.sin((la2 - la1) / 2) ** 2 + math.cos(la1) * math.cos(la2) * math.sin((lo2 - lo1) / 2) ** 2
    return 2 * 6371.0 * math.asin(min(1.0, math.sqrt(h)))
# 旧の版の名簿（作者 2026-10-15 の裁定〈重要でない北極などの州は例外〉の AI の読み。(368) でヌーク、(582)(584) で残り3州＝指示文に写された作者の言葉〈例外はそれでよい〉2026-10-26・docs に原文は無い）
EXEMPT_OLD = {'ヌーク', 'ナイン', 'イカルイト', 'フォートシンプソン'}
# 規則の版の定数（AI の推奨・作者の承認は未確認）。緯度55度は北極圏〔66.6度〕でなく亜北極までを含める＝植民地格の州の緯度は 56.5 と 53.5 の間に空きがある（ナイン 56.5・エドモントン 53.5）。
# 開幕の人口20千人は 12.1（フォートユーコン）と 30.3（セントマイケル）の間に空きがある＝セントマイケル・アラスカ（開幕 30・45 千人・ピーク 120・178 千人の中央）は例外にしない。
RULE_LAT = 55.0
RULE_OPEN_K = 20.0
RULE_TEXT = 'places.json が植民地格・緯度の絶対値 %g 度以上・開幕の人口（world.json の popK）%g 千人未満' % (RULE_LAT, RULE_OPEN_K)
def take_ex(args):
    """引数から `-ex old|rule` を取り除く。戻り＝(版, 残りの引数)。(722) 既定は rule。"""
    if '-ex' not in args: return 'rule', args
    i = args.index('-ex')
    if i + 1 >= len(args) or args[i + 1] not in ('old', 'rule'): raise SystemExit('-ex の後ろは old か rule')
    return args[i + 1], args[:i] + args[i + 2:]
def is_exempt(mode, reg):
    if mode == 'old': return reg in EXEMPT_OLD
    return KIND.get(reg) == 'colony' and abs(PL[reg][0]) >= RULE_LAT and POPK.get(reg, float('nan')) < RULE_OPEN_K   # NaN は < で偽（world.json に無い州は例外にしない）
