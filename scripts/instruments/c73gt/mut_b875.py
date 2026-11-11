# (875) c73gt M2 の写しを作る：stoplines875.sh の判定の行（gdp_zero10 の呼び名 M= を Mx= に）を 1 字変え、cd の行を worktree の位置へ固定する（写しを別の場所に置くため）。
#   python -X utf8 -I mut_b875.py <stoplines875.sh> <出力>
import sys
s = open(sys.argv[1], 'rb').read().decode('utf-8')
a = 'gdp_zero10.py M=$M'
assert s.count(a) == 1
s = s.replace(a, 'gdp_zero10.py Mx=$M')
c = 'cd "$(cd "$(dirname "${BASH_SOURCE[0]}")/../../.." && pwd)"; S=scripts/instruments'
assert s.count(c) == 1
s = s.replace(c, 'cd D:/PoB_work/wt_c73gt; S=scripts/instruments')
open(sys.argv[2], 'wb').write(s.encode('utf-8'))
