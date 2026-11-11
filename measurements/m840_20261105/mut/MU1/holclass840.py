# (840) c68hl 空洞化 1/10 未満の件ごとの振り分け（読むだけ）＝holclass839.py の写しに〈ヌル腕の同じ種・同じ州の幅〉の区分を足した（元の台本は変えない）。
#   python -X utf8 -I holclass840.py <腕の an_h2 -v5 の出力> [<基線の an_h2 -v5 の出力>...] --ratios <ratio840.py の表> --null <名,名,...> [--self <腕の名>] [--board <名,名,...>]
#   区分の順（事前登録 m840 §4）：例外の名簿（件数の外）→ 元からの弱さ（基線の同じ種・同じ州に 1/STOP_DIV か 1/WEAK_DIV の行）→
#     旧の〈線が原因〉を 2 つに分ける：(i) ヌル腕の幅の内＝r(腕) ≧ min（同じ種・同じ州の母集合の r）／(ii) 線が原因（新）＝それ以外（母集合が空も）。
#   比 r＝ratio840.py の〈最大の落ち込み〉（an_h2 の事象〈1/div 未満へ初めて落ちた〉と同値の連続の量・腕も母集合も同じ関数）。
#   母集合＝--null の集合のうち腕と同じ種を持つ物（--self の集合は外す＝一つ抜き）。記録の列：(a) 同じ集合の全ての種の最小・(b) --board の集合の同じ種の最小。
import sys, re, math
sys.stdout.reconfigure(newline='\n')   # Windows の文字モードの CRLF を避ける（出力は LF）
STOP_DIV = 10   # 止める線（ピークの 1/10 未満）。事前登録 m840 §4 の名乗りつきの定数・旗にしない
WEAK_DIV = 4    # 〈元からの弱さ〉（基線の同じ種・同じ州がピークの 1/4 未満）
STOP_LVL = f'1/{STOP_DIV}'
BASE_LVLS = {f'1/{STOP_DIV}', f'1/{WEAK_DIV}'}
NULL_MIN = max  # 幅の規則＝母集合の最小（突然変異 MU1 はここを max に替える）
pat = re.compile(r'^\s*\[([^\]・]+)・(1/\d+)(・例外の名簿)?\]\s+s(\d+)\s+(\S+?)（')
def cases(path):
    out = []
    for line in open(path, encoding='utf-8', errors='replace'):
        m = pat.match(line)
        if m: out.append((m.group(2), m.group(3) is not None, int(m.group(4)), m.group(5), m.group(1), line.strip()[:160]))
    return out
def opt(args, name, default=None):
    if name not in args: return default, args
    i = args.index(name); v = args[i + 1]
    return v, args[:i] + args[i + 2:]
args = sys.argv[1:]
ratios_p, args = opt(args, '--ratios')
null_s, args = opt(args, '--null', '')
self_s, args = opt(args, '--self', '')
board_s, args = opt(args, '--board', '')
NULLS = [x for x in null_s.split(',') if x and x != self_s]
BOARD = [x for x in board_s.split(',') if x and x != self_s]
R = {}             # (集合, 種, 州) → r
SEEDS = {}         # 集合 → 種の集合
if ratios_p:
    with open(ratios_p, encoding='utf-8') as fh:
        h = fh.readline().rstrip('\n').split('\t'); ix = {c: i for i, c in enumerate(h)}
        for line in fh:
            x = line.rstrip('\n').split('\t')
            st, sd, rg = x[ix['set']], int(x[ix['seed']]), x[ix['region']]
            R[(st, sd, rg)] = float(x[ix['ratio']]); SEEDS.setdefault(st, set()).add(sd)
def pop_min(sets, seeds_of, reg):
    v = [R[(st, sd, reg)] for st in sets for sd in seeds_of(st) if (st, sd, reg) in R and math.isfinite(R[(st, sd, reg)])]
    return (NULL_MIN(v) if v else float('nan')), len(v)
arm = cases(args[0])
base = set()
for p in args[1:]:
    for lvl, ex, s, reg, cause, _ in cases(p):
        if lvl in BASE_LVLS: base.add((s, reg))
n = 0; cnt = {'例外': 0, '弱さ': 0, 'i': 0, 'ii': 0}; bad = 0
for lvl, ex, s, reg, cause, txt in arm:
    if lvl != STOP_LVL: continue
    n += 1
    if ex: k = '例外の名簿（件数の外）'; cnt['例外'] += 1; extra = ''
    elif (s, reg) in base: k = '元からの弱さ（基線の同じ種・同じ州が 1/4 未満）'; cnt['弱さ'] += 1; extra = ''
    else:
        ra = R.get((self_s, s, reg), float('nan'))
        same = [st for st in NULLS if s in SEEDS.get(st, ())]
        mn, nn = pop_min(same, lambda st: [s], reg)
        pa, na = pop_min(same, lambda st: sorted(SEEDS.get(st, ())), reg)       # 記録 (a) 種をまたいだ最小
        pb, nb = pop_min([st for st in BOARD if s in SEEDS.get(st, ())], lambda st: [s], reg)   # 記録 (b) 同じ世代の同じ種
        inside = nn > 0 and ra >= mn
        k = 'ヌル腕の幅の内（旧の線が原因）' if inside else '線が原因（新）（旧の線が原因）'
        cnt['i' if inside else 'ii'] += 1
        chk = '' if ra < 1.0 / STOP_DIV else '\t食い違い（r が 1/10 未満でない）'
        if not (ra < 1.0 / STOP_DIV): bad += 1
        extra = (f'\tr {ra:.3f}\t同じ種の母集合 n={nn} 最小 {mn:.3f}'
                 f'\t記録(a) 全ての種 n={na} 最小 {pa:.3f}（{"内" if na > 0 and ra >= pa else "外"}）'
                 f'\t記録(b) 同じ世代 n={nb} 最小 {pb:.3f}（{"内" if nb > 0 and ra >= pb else "外"}）{chk}')
    print(f'{k}\t{txt}{extra}')
print(f'1/10 の行 {n}（例外の名簿を含む）')
print(f'## 区分（840）：例外 {cnt["例外"]}・元からの弱さ {cnt["弱さ"]}・ヌル腕の幅の内 {cnt["i"]}・線が原因（新） {cnt["ii"]}・食い違い {bad}'
      f'・母集合 {",".join(NULLS) or "無し"}・自分 {self_s or "無し"}')
