"""(1074) ledger_S23.tsv の点検：12 列・id の重複 0・判定の型が §0 の 6 種・錨の行に名を含む（7bb40c772）・〈消す〉の行は読み手の数を持つ。"""
import subprocess, re
REPO = 'C:/Users/dirge/Documents/PriceOfBread'; C = '7bb40c772'
TYPES = {'届かない門', '読まれない欄', '天井の錠', '片側だけの門', '両側の門', '効き'}
cache = {}
def lines(f):
    if f not in cache:
        p = subprocess.run(['git', '-C', REPO, 'show', f'{C}:PriceOfBread.SimCore/{f}'], capture_output=True)
        cache[f] = p.stdout.decode('utf-8', 'replace').split('\n') if p.returncode == 0 else None
    return cache[f]
L = open('D:/PoB_work/r1074_work/ledger_S23.tsv', encoding='utf-8').read().splitlines()
bad = []; ids = set(); n = 0
for i, l in enumerate(L[1:], 2):
    c = l.split('\t'); n += 1
    if len(c) != 12: bad.append(f'{i}: 列 {len(c)}'); continue
    if c[0] in ids: bad.append(f'{i}: id の重複 {c[0]}')
    ids.add(c[0])
    if c[6] not in TYPES: bad.append(f'{i}: 型 {c[6]}')
    for a in c[4].split(';'):
        if a == '—': continue
        f, ln, nm = a.split(':', 2); Ls = lines(f)
        if Ls is None or not (0 < int(ln) <= len(Ls)) or not re.search(r'\b' + re.escape(nm) + r'\b', Ls[int(ln) - 1]): bad.append(f'{i}: 錨 {a}')
    if '消す' in c[11] and '読み手' not in c[11]: bad.append(f'{i}: 消すの行に読み手の数が無い')
print(f'行 {n}・落ちた {len(bad)}'); print('\n'.join(bad))
