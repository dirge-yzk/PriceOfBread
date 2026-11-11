# (1029) 台本 sp_eq：基準の cscx_s（keep_1027/base_ins）と新の腕の cscx_s を行ごとに突き合わせる。
#   使い方：python -I sp_eq.py <基準の計器の出力フォルダ> <新の計器の出力フォルダ>
#   通す条件（全行）：rows 不変・fills_new+skipped=fills_old・invalid_new+skipped=invalid_old・toBest 不変・行の鍵の集合が同じ。
#   csc_s は fills 列だけが減ってよい（行数・他の列は不変）。終了コード 0＝全部通る。
import sys, os, glob, csv
old_d, new_d = sys.argv[1], sys.argv[2]
bad = 0; rows = 0; skipped_total = 0; fills_old_total = 0; inv_old_total = 0
files = sorted(glob.glob(os.path.join(old_d, 'cscx_s*.tsv')))
if not files: print('基準の cscx_s が無い'); sys.exit(2)
def read(p):
    with open(p, encoding='utf-8', newline='') as f:
        r = list(csv.reader(f, delimiter='\t'))
    return r[0], r[1:]
for fo in files:
    fn = os.path.join(new_d, os.path.basename(fo))
    if not os.path.exists(fn): print('新に無い', fn); bad += 1; continue
    ho, ro = read(fo); hn, rn = read(fn)
    if hn != ho + ['fillsSkippedLB']: print('見出しが違う', fn, hn); bad += 1; continue
    if len(ro) != len(rn): print('行数が違う', fn, len(ro), len(rn)); bad += 1; continue
    ix = {h: i for i, h in enumerate(hn)}
    for a, b in zip(ro, rn):
        rows += 1
        key_o = tuple(a[:4]); key_n = tuple(b[:4])
        sk = int(b[ix['fillsSkippedLB']])
        ok = (key_o == key_n and a[ix['rowsCount']] == b[ix['rowsCount']]
              and int(b[ix['fillsCount']]) + sk == int(a[ix['fillsCount']])
              and int(b[ix['fillsInvalidCount']]) + sk == int(a[ix['fillsInvalidCount']])
              and a[ix['fillsToBestCount']] == b[ix['fillsToBestCount']])
        skipped_total += sk; fills_old_total += int(a[ix['fillsCount']]); inv_old_total += int(a[ix['fillsInvalidCount']])
        if not ok:
            bad += 1
            if bad < 10: print('不一致', fn, a, b)
# csc_s
cbad = 0
for fo in sorted(glob.glob(os.path.join(old_d, 'csc_s*.tsv'))):
    fn = os.path.join(new_d, os.path.basename(fo))
    if not os.path.exists(fn): print('新に無い', fn); cbad += 1; continue
    ho, ro = read(fo); hn, rn = read(fn)
    if ho != hn or len(ro) != len(rn): print('csc_s の見出し・行数が違う', fn); cbad += 1; continue
    fi = ho.index('fillsCount')
    for a, b in zip(ro, rn):
        a2 = a[:fi] + a[fi + 1:]; b2 = b[:fi] + b[fi + 1:]
        if a2 != b2 or int(b[fi]) > int(a[fi]): cbad += 1
print(f'cscx 行 {rows}・不一致 {bad}・csc 不一致 {cbad}・旧の充填 {fills_old_total}・飛ばした {skipped_total}（{skipped_total / max(1, fills_old_total):.4f}）・旧の無効 {inv_old_total}・飛ばし÷無効 {skipped_total / max(1, inv_old_total):.4f}')
sys.exit(0 if bad == 0 and cbad == 0 else 1)
