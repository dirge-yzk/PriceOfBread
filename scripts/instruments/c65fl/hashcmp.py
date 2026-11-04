# (819) c65fl 使い回しの根拠の読み ①（事前登録 [6]）：腕 B の manifest（sha256 *./パス の行）の SimCore の行と、旗 1 の木の SimCore のファイルを突き合わせる（読むだけ）。
#   使い方: python -I hashcmp.py <manifest> <木の根>
#   出力：manifest にあって木で違う・manifest にあって木に無い・木にあって manifest に無い（SimCore の .cs と .csproj・.json）。
import sys, os, hashlib
man, root = sys.argv[1], sys.argv[2]
want = {}
for line in open(man, encoding='utf-8'):
    line = line.rstrip('\n')
    if ' *./' not in line: continue
    h, p = line.split(' *./', 1)
    if p.startswith('PriceOfBread.SimCore/'): want[p] = h
have = {}
base = os.path.join(root, 'PriceOfBread.SimCore')
for d, _, fs in os.walk(base):
    if os.sep + 'bin' in d or os.sep + 'obj' in d: continue
    for f in fs:
        p = os.path.relpath(os.path.join(d, f), root).replace(os.sep, '/')
        have[p] = hashlib.sha256(open(os.path.join(d, f), 'rb').read()).hexdigest()
diff = sorted(p for p in want if p in have and have[p] != want[p])
miss = sorted(p for p in want if p not in have)
extra = sorted(p for p in have if p not in want and (p.endswith('.cs') or p.endswith('.csproj') or p.endswith('.json')))
same = sum(1 for p in want if p in have and have[p] == want[p])
print(f'manifest の SimCore の行 {len(want)}・同じ {same}・違う {len(diff)}・木に無い {len(miss)}・manifest に無い {len(extra)}')
for p in diff: print('  違う', p)
for p in miss: print('  木に無い', p)
for p in extra: print('  manifest に無い', p)
for k in ('PriceOfBread.SimCore/CapitalMarket.cs', 'PriceOfBread.SimCore/InstrumentsCpwd.cs'):
    print(f'  {k}：', '同じ' if k in want and have.get(k) == want[k] else '違う')
