# (966) 問い 4：m947 と m961 の差（kp の軌道）がヌル腕の幅の内か。
# 既にある出力だけを読む（生は読まない）。幅＝{基線,N1,N2} の最小〜最大。a11（種 11〜20）は幅に入れず向きだけ並べる。
import re, sys, io
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')
W = 'D:/PoB_work/r966_work/'
def rd(p): return open(W + p, 'rb').read().decode('utf-8').splitlines()
m = {}
def put(b, k, v): m.setdefault(b, {})[k] = v
for ln in rd('out_ef_sum.txt'):
    c = ln.split('\t'); b = c[0]
    put(b, 'E', int(c[1].split()[1])); put(b, 'F', int(c[2].split()[1]))
    put(b, 'G', int(re.match(r'G (\d+)', c[3]).group(1)))
    mm = re.match(r'E∧F\(±8\) (\d+)（分 (\d+)', c[5]); put(b, 'EF8', int(mm.group(1))); put(b, 'EF8分', int(mm.group(2)))
    put(b, 'F区間', int(c[8].split()[1]))
for ln in rd('out_fepi.txt'):
    c = ln.split('\t'); mm = re.match(r'挿話 (\d+)（分かれた国 (\d+)', c[1])
    n, s = int(mm.group(1)), int(mm.group(2)); put(c[0], '挿話', n); put(c[0], '挿話分', s); put(c[0], '挿話他', n - s)
for ln in rd('out_birth0.txt'):
    c = ln.split('\t'); put(c[0], '生まれた区間', int(re.search(r'(\d+)$', c[1]).group(1)))
for b in ['b947', 'n1_947', 'n2_947', 'b961', 'n1_961', 'n2_961']:
    for k in ['drain8', 'drain8分', 'drain8他']: put(b, k, 0)
for ln in rd('out_drain8_ge1.txt'):
    c = ln.split('\t'); b = c[1]
    m[b]['drain8'] += 1
    m[b]['drain8分' if c[4].startswith('分') else 'drain8他'] += 1
keys = ['E', 'F', 'G', 'EF8', 'EF8分', 'F区間', '挿話', '挿話分', '挿話他', '生まれた区間', 'drain8', 'drain8分', 'drain8他']
print('量\tm947 b/N1/N2\tm961 b/N1/N2\tm961 基線は m947 の幅の\tm947 基線は m961 の幅の\ta11 947→961')
for k in keys:
    a = [m[x][k] for x in ['b947', 'n1_947', 'n2_947']]; z = [m[x][k] for x in ['b961', 'n1_961', 'n2_961']]
    def side(v, w): return '内' if min(w) <= v <= max(w) else ('外（下）' if v < min(w) else '外（上）')
    a11 = '%s→%s' % (m['a11_947'].get(k, '-'), m['a11_961'].get(k, '-'))
    print('%s\t%s\t%s\t%s\t%s\t%s' % (k, '/'.join(map(str, a)), '/'.join(map(str, z)), side(z[0], a), side(a[0], z), a11))
