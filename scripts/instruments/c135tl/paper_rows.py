# (1302) c135tl 項 A の紙の確かめ：基準（INSRAW の treasledger_s）の途中で生まれた国の最初の観測の四半期に _bornTreasury の行を足した腕を作り、
# ident_check.cols_diff(rows_grown=True) が〈基準の行は順序つき部分列・増えた行だけ〉と読むかを見る。値は同じ行の _treasuryEnd−_ledgerSum。突然変異（値を 1 つ変えた腕）は部分列でなくなる。
import sys, os
sys.path.insert(0, 'scripts/instruments/ident')
import ident_check as ic
raw, out = sys.argv[1], sys.argv[2]
os.makedirs(out, exist_ok=True)
CR = chr(13)
tot_add = 0; bad = 0
for s in range(1, 11):
    pa = f'{raw}/treasledger_s{s}.tsv'; pb = f'{out}/treasledger_s{s}.tsv'
    lines = open(pa, encoding='utf-8', newline='').read().split('\n')
    res = []; n_born = 0; tre = None; led = None
    for ln in lines:
        res.append(ln)
        v = ln.rstrip(CR).split('\t')
        if len(v) == 5 and v[3] == '_treasuryEnd': tre = float(v[4])
        if len(v) == 5 and v[3] == '_ledgerSum': led = float(v[4])
        if len(v) == 5 and v[3] == '_residual' and v[4] == 'nan':
            res.append('\t'.join(v[:3] + ['_bornTreasury', repr(tre - led)]) + CR); n_born += 1
    open(pb, 'w', encoding='utf-8', newline='').write('\n'.join(res))
    ic.ROWS_ADDED.clear()
    kn, real, extra = ic.cols_diff(pa, pb, {}, rows_grown=True)
    added = ic.ROWS_ADDED.get(pa)
    mut = res[:]; k = next(j for j, l in enumerate(mut) if '_treasuryEnd' in l); mut[k] = mut[k].rstrip(CR)[:-1] + '9' + CR
    pm = f'{out}/mut{s}.tsv'; open(pm, 'w', encoding='utf-8', newline='').write('\n'.join(mut))
    kn2, real2, _ = ic.cols_diff(pa, pm, {}, rows_grown=True)
    print(f'seed {s}: born={n_born} added={added} real={real} extra={extra} | mutated real={real2}')
    tot_add += added or 0; bad += len(real)
print('total_added', tot_add, 'real_total', bad)
