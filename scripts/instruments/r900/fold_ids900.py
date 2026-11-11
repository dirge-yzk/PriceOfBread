# (900) c77lg：畳む前の台帳と後の台帳の行の id を比べる（読むだけ）。S19 の仮の行（S19-H）と S19 の節の行（S19-nn）は置き換えなので別に数える。
# 使い方: python -X utf8 fold_ids900.py <前の台帳> <後の台帳>
import sys, re, collections
ROW = re.compile(r'^\| (S\d\d-[A-Z]?\d+) \|')
def ids(p): return [m.group(1) for l in open(p, 'rb').read().decode('utf-8').split('\n') for m in [ROW.match(l)] if m]
a, b = ids(sys.argv[1]), ids(sys.argv[2])
def core(x): return [i for i in x if not re.match(r'^S19-(H\d+|\d+)$', i)]
ca, cb = core(a), core(b)
sa = collections.defaultdict(list); sb = collections.defaultdict(list)
for i in ca: sa[i[:3]].append(i)
for i in cb: sb[i[:3]].append(i)
same_set = sorted(ca) == sorted(cb); same_seq = sa == sb
print(f'前 {len(a)} 行（S19-H {sum(1 for i in a if i.startswith("S19-H"))}）・後 {len(b)} 行（S19-nn {sum(1 for i in b if re.match(r"^S19-[0-9]+$", i))}）')
print(f'S19 を除いた id：前 {len(ca)}・後 {len(cb)}・集合が一致 {same_set}・系統ごとの並びが一致 {same_seq}・重複 前 {len(ca) - len(set(ca))} 後 {len(cb) - len(set(cb))}')
sys.exit(0 if same_set and same_seq else 1)
