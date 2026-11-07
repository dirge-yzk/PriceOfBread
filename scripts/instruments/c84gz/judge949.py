# (949) c84gz：止める線〈名目 GDP の 1 四半期 100 倍以上〉の〈それ以外〉の判定（読むだけ）。入力は stoplines906.sh（か 949）が書いた出力ファイル（コミット済みの物でよい＝D: の生を名指さない）。
#   区分の出所＝出力の中の二つの節：3 区分＝「== gdpjump833（3 区分・行つき）」の行・4 区分＝「== 記録（906・新しい区分…gdp_jump906 --founding」の行（★(905) ① で〈建国の後の 0 の続き〉を区分に昇格した読み）。
#   数える区分は --basis 4|3（既定 4＝★(905) ①）。両方を並べて出す。
#   pair：腕の件数 ≦ ヌル腕（--null で渡した出力）の件数の最大 → 記録（rc 0）・超えたら止める（rc 1）。参照が空なら strict に落ちず rc 2（入力の誤り）。
#   strict：1 件でも止める。使い方: python -X utf8 -I judge949.py --rule pair|strict [--basis 4|3] --arm <出力> [--null <出力> ...]
import re, sys
A = sys.argv[1:]
def opt(n, d=None): return A[A.index(n) + 1] if n in A else d
RULE = opt('--rule', 'pair'); BASIS = opt('--basis', '4')
ARM = opt('--arm'); NULLS = [A[i + 1] for i, a in enumerate(A) if a == '--null']
def counts(path):
    t = open(path, encoding='utf-8').read().split('\n'); out = {}
    for key, head in (('3', '== gdpjump833（3 区分'), ('4', '== 記録（906・新しい区分・採否に使わない）：gdp_jump906')):
        i = next((k for k, l in enumerate(t) if l.startswith(head)), None)
        if i is None: out[key] = None; continue
        m = next((re.search(r'それ以外 (\d+)）', l) for l in t[i + 1:i + 6] if re.search(r'^\S+\t種 \d+\t100 倍以上', l)), None)
        out[key] = int(m.group(1)) if m else None
    return out
def main():
    if RULE not in ('pair', 'strict') or BASIS not in ('3', '4') or not ARM: print('引数の誤り'); return 2
    a = counts(ARM); ns = [counts(p) for p in NULLS]
    print(f'# 判定949 rule={RULE} 数える区分={BASIS} 区分（3 区分＝gdpjump833・4 区分＝建国の後の 0 の続きを区分に昇格した読み〔★(905) ①〕）')
    print(f'腕 {ARM}\t〈それ以外〉 3 区分 {a["3"]}・4 区分 {a["4"]}')
    for p, n in zip(NULLS, ns): print(f'ヌル腕 {p}\t〈それ以外〉 3 区分 {n["3"]}・4 区分 {n["4"]}')
    me = a[BASIS]
    if me is None or any(n[BASIS] is None for n in ns): print('入力の誤り：節が読めない'); return 2
    if RULE == 'strict': stop = me > 0; ref = 0
    else:
        if not ns: print('入力の誤り：pair にヌル腕が無い'); return 2
        ref = max(n[BASIS] for n in ns); stop = me > ref
    print(f'参照の最大 {ref}・腕 {me} → ' + ('止める' if stop else '記録'))
    for k in ('3', '4'):
        if k != BASIS and a[k] is not None and all(n[k] is not None for n in ns):
            r2 = 0 if RULE == 'strict' else max([n[k] for n in ns] or [0]); print(f'（並べる：{k} 区分の読みでは 参照 {r2}・腕 {a[k]} → ' + ('止める' if a[k] > r2 else '記録') + '）')
    return 1 if stop else 0
sys.exit(main())
