# (904) c77sm landshare_s の新しい末尾 3 列の門（読むだけ）。
#   python -X utf8 gate904.py <ins の生のフォルダ> <出力 .txt>   ／   python -X utf8 gate904.py --selftest
# 門：(1) 全ての行で maxRelDiffSlotVsActualTierRule ≦ 1e-12
#     (2) 既にある列 maxRelDiffSlotVsActual が 1e-12 を超える行（gate895 の破れ＝種 7 英 q484 の型）は、全て capColonialMonths＞0 かつ remitNotAddedPerQuarter＞0（首都が植民地格の月がある＝破れが説明できる）
#     (3) capColonialMonths＝0 の行では remitNotAddedPerQuarter＝0 かつ maxRelDiffSlotVsActual ≦ 1e-12（本国の格の首都では旧の規則と新しい規則が同じ）
import sys, os, glob, csv, math
TOL = 1e-12

def fl(x):
    try: return float(x)
    except Exception: return math.nan

def check_rows(rows):
    bad = []
    for seed, r in rows:
        key = (seed, r['nation'], r['quarter'])
        tr = fl(r['maxRelDiffSlotVsActualTierRule']); old = fl(r['maxRelDiffSlotVsActual']); cm = fl(r['capColonialMonths']); na = fl(r['remitNotAddedPerQuarter'])
        if not (tr <= TOL): bad.append(key + ('門1 新しい規則の差', r['maxRelDiffSlotVsActualTierRule']))
        if not (old <= TOL) and not (cm > 0 and na > 0): bad.append(key + ('門2 旧の破れが首都の格で説明できない', r['maxRelDiffSlotVsActual']))
        if cm == 0 and (na != 0 or not (old <= TOL)): bad.append(key + ('門3 本国の格の首都で旧と新が違う', r['remitNotAddedPerQuarter']))
    return bad

def selftest():
    base = {'nation': 'X', 'quarter': '0', 'maxRelDiffSlotVsActualTierRule': '0', 'maxRelDiffSlotVsActual': '0', 'capColonialMonths': '0', 'remitNotAddedPerQuarter': '0'}
    ok = check_rows([('1', dict(base))]) == []
    brk = dict(base, maxRelDiffSlotVsActual='0.38', capColonialMonths='3', remitNotAddedPerQuarter='109.4')
    ok2 = check_rows([('7', brk)]) == []                                        # 説明できる破れは通る
    m1 = dict(brk, maxRelDiffSlotVsActualTierRule='0.01')                         # 新しい規則の差が残る
    m2 = dict(base, maxRelDiffSlotVsActual='0.38')                                # 首都の格で説明できない旧の破れ
    m3 = dict(base, remitNotAddedPerQuarter='5')                                  # 本国の格の首都なのに入らなかった額
    caught = [len(check_rows([('1', x)])) >= 1 for x in (m1, m2, m3)]
    print('selftest 綺麗な行・説明できる破れが通る', ok, ok2, '・人工の外れ（新しい規則の差・説明できない破れ・本国の格の入らなかった額）を落とす', caught)
    return 0 if ok and ok2 and all(caught) else 1

def main():
    if len(sys.argv) > 1 and sys.argv[1] == '--selftest': sys.exit(selftest())
    d, out = sys.argv[1], sys.argv[2]
    rows = []
    for p in sorted(glob.glob(os.path.join(d, 'landshare_s*.tsv'))):
        seed = os.path.basename(p)[len('landshare_s'):-4]
        with open(p, encoding='utf-8', newline='') as fh:
            for r in csv.DictReader(fh, delimiter='\t'): rows.append((seed, r))
    bad = check_rows(rows)
    broken = [(s, r['nation'], r['quarter'], r['maxRelDiffSlotVsActual'], r['capColonialMonths'], r['remitNotAddedPerQuarter'], r['maxRelDiffSlotVsActualTierRule']) for s, r in rows if not (fl(r['maxRelDiffSlotVsActual']) <= TOL)]
    capcol = sum(1 for s, r in rows if fl(r['capColonialMonths']) > 0)
    lines = ['# gate904 行 %d・門の破れ %d・旧の列が 1e-12 を超える行 %d・首都が植民地格の月がある行 %d' % (len(rows), len(bad), len(broken), capcol)]
    lines += ['旧の破れの行（種 国 四半期 旧の差 首都の植民地格の月数 入らなかった額 新しい規則の差）：' + '\t'.join(map(str, x)) for x in broken]
    lines += ['門の破れ：' + '\t'.join(map(str, x)) for x in bad]
    open(out, 'w', encoding='utf-8').write('\n'.join(lines) + '\n'); print('\n'.join(lines[:6]))
    sys.exit(1 if bad else 0)

if __name__ == '__main__': main()
