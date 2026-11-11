# (965) 読むだけ：輸出が閉じた出来事（<名札>_close.tsv）を、持ち主の国の本位（monlaw_s の MonetaryStandard＝0 不換・1 銀・2 金）の替わりと突き合わせる。
#   使い方: python -X utf8 -I read965e.py <ins フォルダ> <種の起点> <名札> <出力フォルダ>
#   数えるもの：閉じた四半期 q に本位が 1/2→0 になった出来事（q−1 が金属・q が不換）・同じ国×種×q で 3 州以上が閉じた〈国の出来事〉・
#   本位が金属に戻った後に輸出が再び開いたか（cearn の inExport＞0 が q 以後に現れるか）・0 の続きの州×四半期のうち不換の替わりで閉じた州の分。
import sys, os, collections as C
M, SB, TAG, OUT = sys.argv[1], int(sys.argv[2]), sys.argv[3], sys.argv[4]
close = [l.split('\t') for l in open(os.path.join(OUT, f'{TAG}_close.tsv'), encoding='utf-8').read().split('\n')[1:] if l]
dead = [l.split('\t') for l in open(os.path.join(OUT, f'{TAG}_dead.tsv'), encoding='utf-8').read().split('\n')[1:] if l]
law = {}
for s in range(SB, SB + 10):
    p = os.path.join(M, f'monlaw_s{s}.tsv')
    f = open(p, encoding='utf-8'); h = f.readline().rstrip('\n').split('\t'); ix = {n: i for i, n in enumerate(h)}
    for l in f:
        r = l.rstrip('\n').split('\t')
        try: law[(s, r[ix['nation']], int(r[ix['quarter']]))] = int(r[ix['MonetaryStandard']])
        except (ValueError, IndexError): pass
grp = C.Counter((r[0], r[5], r[2]) for r in close)
S = C.Counter(); fiatClose = set(); back = C.Counter()
for r in close:
    s, rn, q, nat = int(r[0]), r[1], int(r[2]), r[5]
    lb, la = law.get((s, nat, q - 1)), law.get((s, nat, q))
    natEv = grp[(r[0], r[5], r[2])] >= 3
    fiat = lb in (1, 2) and la == 0
    S['n'] += 1; S['natEv'] += natEv; S['fiat'] += fiat; S['fiat_natEv'] += fiat and natEv
    if fiat:
        fiatClose.add((s, rn, q))
        qb = next((qq for qq in range(q + 1, 669) if law.get((s, nat, qq)) in (1, 2)), None)
        back['戻った' if qb is not None else '戻らない'] += 1
        if qb is not None: fiatClose.add((s, rn, -qb))
# 戻った後に輸出が再び開いたか（cearn を州ごとに読む）
need = {(s, rn): q for (s, rn, q) in fiatClose if q > 0}
backq = {(s, rn): -q for (s, rn, q) in fiatClose if q < 0}
reopen = C.Counter()
for s in range(SB, SB + 10):
    want = {rn for (ss, rn) in need if ss == s}
    if not want: continue
    f = open(os.path.join(M, f'cearn_s{s}.tsv'), encoding='utf-8'); h = f.readline().rstrip('\n').split('\t'); ix = {n: i for i, n in enumerate(h)}
    first = {}
    for l in f:
        r = l.rstrip('\n').split('\t')
        rn = r[ix['region']]
        if rn not in want: continue
        try: q = int(r[ix['quarter']]); e = float(r[ix['inExportPerQuarter']])
        except (ValueError, IndexError): continue
        b = backq.get((s, rn))
        if b is not None and q >= b and e > 0 and rn not in first: first[rn] = q
    for (ss, rn), q in need.items():
        if ss != s: continue
        b = backq.get((s, rn))
        reopen['本位が戻らない' if b is None else ('戻った後に開いた' if rn in first else '戻った後も開かない')] += 1
# 0 の続きの州×四半期のうち、最後に閉じた出来事が不換の替わりだった州
fc = {(int(r[0]), r[1]): sorted(int(x[2]) for x in close if x[0] == r[0] and x[1] == r[1]) for r in dead}
dq = C.Counter()
for r in dead:
    s, rn, q0, ln = int(r[0]), r[1], int(r[2]), int(r[3])
    cs = [q for q in fc[(s, rn)] if q <= q0]
    k = 'なし' if not cs else ('不換' if (s, rn, cs[-1]) in fiatClose else ('国の出来事' if grp[(str(s), next(x[5] for x in close if x[0] == str(s) and x[1] == rn and int(x[2]) == cs[-1]), str(cs[-1]))] >= 3 else 'その他'))
    if cs and cs[-1] <= 8 and k == 'その他': k = '開幕の 8 四半期'
    dq[k] += ln
o = open(os.path.join(OUT, f'{TAG}_fiat.txt'), 'w', encoding='utf-8')
print(f'{TAG}\t閉じた出来事 {S["n"]}\t国の出来事（同じ国×種×四半期で 3 州以上） {S["natEv"]}\t不換への替わりと同じ四半期 {S["fiat"]}（うち国の出来事 {S["fiat_natEv"]}）\t本位 {dict(back)}\t輸出 {dict(reopen)}', file=o)
print(f'{TAG}\t0 の続きの州×四半期を最後に閉じた出来事で分ける\t{dict(dq)}\t計 {sum(dq.values())}', file=o)
o.close(); print(open(os.path.join(OUT, f'{TAG}_fiat.txt'), encoding='utf-8').read())
