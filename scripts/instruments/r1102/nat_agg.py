# (1102) 国の段の集め：rulepower_s の行から、種×四半期×国の L・W（前線 A）・H・O・海軍・人口K の和を 1 行に集める（表を読むだけ・エンジンを呼ばない）。
# 使い方: python -X utf8 -I nat_agg.py <表のフォルダ> <出力.tsv>
# 人口K の和は、その国の全ての州の popK の和（InstrumentsRulePower.cs:173 の SumPop と同じ母集合）。L・W・H・O は国の列（州の行で同じ値）を最初の行から取る。
import sys, os
d, outp = sys.argv[1], sys.argv[2]
out = open(outp, 'w', encoding='utf-8', newline='\n')
out.write('seed\tquarter\tyear\tnation\tL\tWA\tH\tO\tnavy\tpopK\tnReg\n')
for s in range(1, 11):
    p = os.path.join(d, f'rulepower_s{s}.tsv')
    if not os.path.exists(p): continue
    with open(p, encoding='utf-8') as fi:
        h = fi.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        iq, iy, inm, ip = ix['quarter'], ix['year'], ix['nation'], ix['popK']
        iL, iW, iH, iO, iN = ix['lNationIndex'], ix['wNationIndex'], ix['hNationIndex'], ix['oNationIndex'], ix['navalNationIndex']
        cur = None; acc = {}
        def flush():
            for nm, v in acc.items():
                out.write(f'{s}\t{cur}\t{v[0]}\t{nm}\t{v[1]}\t{v[2]}\t{v[3]}\t{v[4]}\t{v[5]}\t{v[6]!r}\t{v[7]}\n')
        for line in fi:
            c = line.rstrip('\n').split('\t')
            q = c[iq]
            if q != cur:
                if cur is not None: flush()
                cur = q; acc = {}
            nm = c[inm]
            v = acc.get(nm)
            if v is None:
                v = [c[iy], c[iL], c[iW], c[iH], c[iO], c[iN], 0.0, 0]; acc[nm] = v
            try: pk = float(c[ip])
            except ValueError: pk = 0.0
            if pk > 0: v[6] += pk
            v[7] += 1
        if cur is not None: flush()
out.close()
