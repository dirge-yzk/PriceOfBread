# (778) flow_s の呼び出し位置（sourceFile・sourceLine）を除いた行の多重集合の比べ（ident_check.py の flow_same_but_location をそのまま使う・読むだけ）
#   python -X utf8 flowcmp.py <腕の ins> <比べる側の ins>
import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'ident'))
from ident_check import flow_same_but_location
a, b = sys.argv[1], sys.argv[2]
fs = sorted(f for f in os.listdir(a) if f.startswith('flow_s') and f.endswith('.tsv'))
ok = [f for f in fs if flow_same_but_location(os.path.join(a, f), os.path.join(b, f))]
print(f'flow_s：呼び出し位置を除いた多重集合が同じ {len(ok)}/{len(fs)}', '判定: 一致' if len(ok) == len(fs) else '判定: 不一致 ' + ','.join(sorted(set(fs) - set(ok))))
