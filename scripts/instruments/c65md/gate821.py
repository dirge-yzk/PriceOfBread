# (821) c65md 門の読み手：計器 cpmed_s（講和 1 本ごとに 1 行・旗 0 でも旗 1 でも同じ列）と cpeace_s（崩壊の事象ごと）から、事前登録 measurements/m821_20261104_prereg_md.txt の門を読む。
#   使い方: python -I -X utf8 gate821.py <ins フォルダ>
#   G0 働いた：調停の戦争で崩壊と判定された外交の講和（forced=1・collapsedIter=1・civil=0）が 1 本以上・その Σ repWouldOld ＞ 0
#   G1 下限：forced=1・civil=0 の全ての講和で Σ repMoved＝0・Σ ledgerAbs ≦1e-9・Σ captured＝0・Σ ceded＝0・Σ regionsMoved＝0・hit＝0
#   G2 両側：cpeace_s の Σ repMovedIndex ÷ Σ repExpectIndex が [0.95, 1.05]／forced=0・civil=0・collapsedIter=1 の講和で overwriteMatch＝0 の本数＝0
#   母集合が空なら〈母集合が空〉と出して落とす（G0・G2 の比）＝何もしない計器でも通る形にしない。
import sys, os, glob, csv
d = sys.argv[1]
def F(x):
    try: return float(x)
    except Exception: return float('nan')
def seed_of(p, head):
    return int(os.path.basename(p)[len(head):-4])
rows = []
for f in sorted(glob.glob(os.path.join(d, 'cpmed_s*.tsv')), key=lambda p: seed_of(p, 'cpmed_s')):
    s = seed_of(f, 'cpmed_s')
    with open(f, encoding='utf-8') as fh:
        for r in csv.DictReader(fh, delimiter='\t'):
            r['seed'] = s; rows.append(r)
cp = []
for f in sorted(glob.glob(os.path.join(d, 'cpeace_s*.tsv')), key=lambda p: seed_of(p, 'cpeace_s')):
    with open(f, encoding='utf-8') as fh:
        cp.extend(csv.DictReader(fh, delimiter='\t'))
seeds = sorted(set(r['seed'] for r in rows))
print(f'# {d}・cpmed_s の種 {len(seeds)}・講和 {len(rows)}・cpeace_s の事象 {len(cp)}')
ok = True
def gate(name, val, cond, note=''):
    global ok
    p = bool(cond); ok = ok and p
    print(('通る' if p else '落ちる') + '\t' + name + '\t' + str(val) + ('\t' + note if note else ''))
ext = [r for r in rows if r['civilFlag'] == '0']
fc = [r for r in ext if r['forcedFlag'] == '1' and r['collapsedIterFlag'] == '1']
would = sum(F(r['repWouldOldIndex']) for r in fc)
gate('G0 働いた：調停の戦争で崩壊と判定された講和の本数 ≧1 かつ Σ旧の規則の反実仮想 ＞0', f'{len(fc)} 本・Σ {would:.6g}', len(fc) >= 1 and would > 0,
     '母集合が空' if not fc else '種ごと ' + ','.join(f'{s}:{sum(1 for r in fc if r["seed"] == s)}' for s in seeds))
fa = [r for r in ext if r['forcedFlag'] == '1']
rep = sum(F(r['repMovedIndex']) for r in fa); led = sum(F(r['ledgerAbsIndex']) for r in fa)
cap = sum(int(r['capturedCount']) for r in fa); ced = sum(int(r['cededFlag']) for r in fa)
reg = sum(int(r['regionsMovedCount']) for r in fa); hit = sum(int(r['hitFlag']) for r in fa)
gate('G1 調停の戦争の講和：Σ賠償＝0・Σ台帳 ≦1e-9・植民地 0・割譲 0・州 0・賠償の枝 0',
     f'Σ賠償 {rep:.6g}・Σ台帳 {led:.6g}・植民地 {cap}・割譲 {ced}・州 {reg}・枝 {hit}',
     rep == 0 and led <= 1e-9 and cap == 0 and ced == 0 and reg == 0 and hit == 0, f'調停の戦争の講和 {len(fa)} 本（母集合が空なら G0 が落ちる）')
sm = sum(F(e['repMovedIndex']) for e in cp); se = sum(F(e['repExpectIndex']) for e in cp)
ratio = sm / se if se > 0 else float('nan')
gate('G2a 崩壊の事象 Σ実際÷Σ期待（同じ盤面）∈[0.95,1.05]', f'{ratio:.6f}', se > 0 and 0.95 <= ratio <= 1.05, f'Σ実際 {sm:.6g}・Σ期待 {se:.6g}' + ('・母集合が空' if not cp else ''))
nf = [r for r in ext if r['forcedFlag'] == '0' and r['collapsedIterFlag'] == '1']
nm = sum(1 for r in nf if r['overwriteMatchFlag'] != '1')
gate('G2b 調停でない崩壊の講和で上書きの式と一致しない本数＝0', nm, len(nf) > 0 and nm == 0, f'母集合 {len(nf)} 本' + ('・母集合が空' if not nf else ''))
print('総合\t' + ('全て通る' if ok else '落ちる門あり'))
tot = sum(F(e['repMovedIndex']) for e in cp)
print(f'[記録] 調停の戦争の講和 {len(fa)} 本（うち崩壊 {len(fc)}）・Σ旧の規則の反実仮想 {would:.6g}・崩壊の講和の Σ実際の賠償 {tot:.6g}・割合 {(would / tot if tot > 0 else float("nan")):.4%}')
for r in fc:
    print(f'[事象] 種 {r["seed"]}・{r["year"]}・{r["sideA"]}×{r["sideB"]}・戦勝点 {r["scoreIndex"]}・賠償 {r["repMovedIndex"]}・反実仮想 {r["repWouldOldIndex"]}・枝 {r["hitFlag"]}・植民地 {r["capturedCount"]}・割譲 {r["cededFlag"]}')
