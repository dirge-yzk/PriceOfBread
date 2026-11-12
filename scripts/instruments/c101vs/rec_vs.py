# (1064) c101vs 記録（採否に使わない）：試合の終わりの属国のリンクの数（dep_s・旗 0 と腕 B）と、戦争の行の数（wars_s・腕 B・基線・N1・N2）。
#   python rec_vs.py <旗 0 の生（dep_s あり）> <腕 B の生> <基線の生> <N1 の生> <N2 の生>
#   限界（自己申告）：N1・N2 に dep_s は無い＝属国の数の幅は出ない（旗 0 と腕 B の比だけ）。wars_s は内戦（civilWarFlag=1）を分けて数える。
import csv, glob, os, sys

def rows(p):
    with open(p, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r)
        return h, list(r)

def seeds(d, t):
    out = []
    for p in glob.glob(os.path.join(d, f'{t}_s*.tsv')):
        b = os.path.basename(p)[len(t) + 2:-4]
        if b.isdigit(): out.append(int(b))
    return sorted(out)

def endlinks(d):
    per = {}
    for s in seeds(d, 'dep'):
        h, rs = rows(os.path.join(d, f'dep_s{s}.tsv'))
        k, la = h.index('kindCode'), h.index('linksAfterCount')
        end = [r for r in rs if r[k] == '2']
        per[s] = int(end[-1][la]) if end else None
    return per

def wars(d):
    per = {}
    for s in seeds(d, 'wars'):
        h, rs = rows(os.path.join(d, f'wars_s{s}.tsv'))
        c = h.index('civilWarFlag')
        per[s] = (sum(1 for r in rs if r[c] != '1'), sum(1 for r in rs if r[c] == '1'))
    return per

f0, b, m, n1, n2 = sys.argv[1:6]
e0, eb = endlinks(f0), endlinks(b)
print('試合の終わりの属国のリンクの数（dep_s）')
print('  旗 0 ', ' '.join(f's{s}={e0[s]}' for s in sorted(e0)), '合計', sum(v for v in e0.values() if v is not None))
print('  腕 B ', ' '.join(f's{s}={eb[s]}' for s in sorted(eb)), '合計', sum(v for v in eb.values() if v is not None))
print('戦争の行の数（wars_s・内戦を除く／内戦）10 種の合計')
tot = {}
for name, d in (('腕 B', b), ('基線', m), ('N1', n1), ('N2', n2)):
    w = wars(d); a = sum(v[0] for v in w.values()); c = sum(v[1] for v in w.values()); tot[name] = a
    print(f'  {name} 種 {len(w)}  国家間 {a}  内戦 {c}')
vals = [tot['基線'], tot['N1'], tot['N2']]; wd = max(vals) - min(vals)
lo, hi = min(vals) - wd, max(vals) + wd
print(f'  国家間の帯（基線±ヌル腕の幅）[{lo}, {hi}]・腕 B {tot["腕 B"]}＝{"中" if lo <= tot["腕 B"] <= hi else "外"}（記録）')
