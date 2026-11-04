# (840) c68hl 両側の門：小さな盤面の holclass840 の出力を、事前登録 m840 §7 の期待と比べる（期待は盤面の意図から書いた）
#   python -X utf8 -I fixgate840.py <holclass840 の出力>...
import sys, re
sys.stdout.reconfigure(newline='\n')   # Windows の文字モードの CRLF を避ける（出力は LF）
EXP = {'マドリード': 'ヌル腕の幅の内', 'バレンシア': '線が原因（新）', 'ダブリン': '元からの弱さ', 'ナイン': '例外の名簿'}
EXP_N = 4   # 1/10 の行（例外の名簿を含む）＝リスボン（1/4 だけ）は入らない
for p in sys.argv[1:]:
    got = {}; n = None
    for l in open(p, encoding='utf-8'):
        m = re.match(r'^1/10 の行 (\d+)', l)
        if m: n = int(m.group(1)); continue
        f = l.rstrip('\n').split('\t')
        if len(f) < 2: continue
        m = re.search(r'\]\s+s\d+\s+(\S+?)（', f[1])
        if not m: continue
        rg = m.group(1); k = next((c for c in ('ヌル腕の幅の内', '線が原因（新）', '元からの弱さ', '例外の名簿') if f[0].startswith(c)), f[0])
        got.setdefault(rg, []).append(k)
    bad = [f'{r}:{EXP[r]}→{"/".join(got.get(r, ["無し"]))}' for r in EXP if got.get(r) != [EXP[r]]]
    bad += [f'{r}:余分→{"/".join(v)}' for r, v in got.items() if r not in EXP]
    if n != EXP_N: bad.append(f'1/10 の行 {EXP_N}→{n}')
    print(f'{p}\t{"緑" if not bad else "赤"}\t{" ".join(bad) if bad else "-"}')
