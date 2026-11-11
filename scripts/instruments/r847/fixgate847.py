# (847) c69hl 両側の門：小さな盤面（mkfix847）の出力を、事前登録 m847 §6 の期待と比べる（期待は盤面の意図から書いた＝台本の出力から写さない）
#   python -X utf8 -I fixgate847.py A <盤 A の holclass の出力>   ／  B <盤 B の holclass の出力>  ／  L <盤 L の loo847 の出力>
import sys, re
sys.stdout.reconfigure(newline='\n')   # Windows の文字モードの CRLF を避ける（出力は LF）
I, II, WK, EX = 'ヌル腕の幅の内', '線が原因（新）', '元からの弱さ', '例外の名簿'
EXP = {
    'A': ({'マドリード': I, 'バレンシア': II, 'ダブリン': WK, 'ナイン': EX, 'セビリア': II, 'ビルバオ': II, 'コルドバ': II}, 7),
    'B': ({'マドリード': II, 'セビリア': II}, 2),
}
def hcgate(board, p):
    exp, en = EXP[board]
    got = {}; n = None
    for l in open(p, encoding='utf-8'):
        m = re.match(r'^1/10 の行 (\d+)', l)
        if m: n = int(m.group(1)); continue
        f = l.rstrip('\n').split('\t')
        if len(f) < 2: continue
        m = re.search(r'\]\s+s\d+\s+(\S+?)（', f[1])
        if not m: continue
        k = next((c for c in (I, II, WK, EX) if f[0].startswith(c)), f[0])
        got.setdefault(m.group(1), []).append(k)
    bad = [f'{r}:{exp[r]}→{"/".join(got.get(r, ["無し"]))}' for r in exp if got.get(r) != [exp[r]]]
    bad += [f'{r}:余分→{"/".join(v)}' for r, v in got.items() if r not in exp]
    if n != en: bad.append(f'1/10 の行 {en}→{n}')
    return bad
def lgate(p):
    t = open(p, encoding='utf-8').read()
    bad = []
    # 一つ抜き：腕k  帯  c(k)  参照  thr  T1  T2  T3
    LOO = {'nA': ('2', '0', '止める', '止める', '止める'), 'nB': ('0', '2', '-', '-', '-'), 'nC': ('0', '2', '-', '-', '-')}
    for k, e in LOO.items():
        m = re.search(rf'^{k}\t[^\t]*\t(\d+)\t[^\t]*\t(\S+)\t(\S+)\t(\S+)\t(\S+)$', t, re.M)
        g = m.groups() if m else None
        if g != e: bad.append(f'一つ抜き {k}:{"/".join(e)}→{"/".join(g) if g else "無し"}')
    m = re.search(r'## 空振り T1 (\d+)/(\d+)・T2 (\d+)/\d+・T3 (\d+)/\d+', t)
    g = m.groups() if m else None
    if g != ('1', '3', '1', '1'): bad.append(f'空振り 1/3・1/3・1/3→{g}')
    m = re.search(r'## 参照（9 組・自分を外した件数）(\S+)・thr (\d+)', t)
    g = m.groups() if m else None
    if g != ('nA:2,nB:0,nC:0', '2'): bad.append(f'参照 nA:2,nB:0,nC:0・thr 2→{g}')
    m = re.search(r'^arm\t[^\t]*\t\d+\t\d+\t\d+\t(\d+)\t\d+\t(\S+)\t(\S+)\t[^\t]*\t[^\t]*\t(\S+)\t', t, re.M)
    g = m.groups() if m else None
    if g != ('4', '止める', '止める（4・2）', '止める'): bad.append(f'腕 arm:4/T1 止める/T2 止める（4・2）/T3 止める→{g}')
    return bad
board, p = sys.argv[1], sys.argv[2]
bad = lgate(p) if board == 'L' else hcgate(board, p)
print(f'盤 {board}\t{"緑" if not bad else "赤"}\t{" ".join(bad) if bad else "-"}')
