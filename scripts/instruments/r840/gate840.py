# (840) c68hl 恒等の門（旧の既定で恒等）：同じ入力の holclass839.py と holclass840.py の出力を比べる（事前登録 m840 §7）
#   python -X utf8 -I gate840.py <名> <holclass839 の出力> <holclass840 の出力>
#   一致＝1/10 の行の数・例外の名簿の行・元からの弱さの行・〈(i)＋(ii)〉の行の多重集合＝旧の〈線が原因〉の行の多重集合。
import sys, re, collections
sys.stdout.reconfigure(newline='\n')   # Windows の文字モードの CRLF を避ける（出力は LF）
def read(p):
    n = None; C = collections.defaultdict(collections.Counter)
    for l in open(p, encoding='utf-8'):
        m = re.match(r'^1/10 の行 (\d+)', l)
        if m: n = int(m.group(1)); continue
        f = l.rstrip('\n').split('\t')
        if len(f) < 2 or not f[1].startswith('['): continue
        k = f[0]
        if k.startswith('例外'): c = '例外'
        elif k.startswith('元からの弱さ'): c = '弱さ'
        elif k.startswith('線が原因（基線') or k.startswith('ヌル腕の幅の内') or k.startswith('線が原因（新）'): c = '線'
        else: c = '不明:' + k
        C[c][f[1]] += 1
    return n, C
name, po, pn = sys.argv[1:4]
no, Co = read(po); nn, Cn = read(pn)
diff = []
if no != nn: diff.append(f'1/10 の行 {no}≠{nn}')
for c in sorted(set(Co) | set(Cn)):
    if Co[c] != Cn[c]: diff.append(f'{c} の行が違う（旧 {sum(Co[c].values())}・新 {sum(Cn[c].values())}）')
print(f'{name}\t{"一致" if not diff else "不一致"}\t1/10 {no}/{nn}\t例外 {sum(Co["例外"].values())}\t弱さ {sum(Co["弱さ"].values())}\t線 {sum(Co["線"].values())}\t{" ".join(diff) if diff else "-"}')
