# (893) c76lg：下側の門の自己試験の台帳を作る（バイトで読み書き）。
# 使い方: python -X utf8 mk_fixtures893.py <台帳 after> <出力フォルダ>
import sys, os
src, od = sys.argv[1:3]
os.makedirs(od, exist_ok=True)
b = open(src, 'rb').read().decode('utf-8')
L = b.split('\n')
def w(name, text): open(os.path.join(od, name), 'wb').write(text.encode('utf-8'))
w('ledger_empty.md', '')
w('ledger_noS16.md', '\n'.join(l for l in L if not l.startswith('| S16-')))
a = 'Politics.cs:398:double erosion ='
assert b.count(a) == 1
w('ledger_shiftS15.md', b.replace(a, 'Politics.cs:399:double erosion ='))
prov = '| S15-H001 | S15 | 正統性（政府の体力） | 文書 L2593（粒＝見出し・未分割） | Coefficients.cs:376:LegitimacyErosionRate | Politics.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | — |'
k = b.index('| S15-45 |'); e = b.index('\n', k)
w('ledger_provS15.md', b[:e + 1] + prov + '\n' + b[e + 1:])
w('ledger_after.md', b)
print('fixtures', sorted(os.listdir(od)))
