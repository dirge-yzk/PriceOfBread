# (1150) 合流：c112hy の門 G4 を種 11〜20 で読む（gates.py の G4 と同じ式と幅＝開戦÷講和が〈旗 0 の開戦÷講和×(1±0.0072)〉の内・基準の値だけを同じ種の旗 0 の生 S11z から取る＝予言 [4]）。
#   python -X utf8 -I g4s11.py <腕の ins> <旗 0 の ins> <種の始め> <種の終わり>
import sys, os
ARM, OFF, S0, S1 = sys.argv[1], sys.argv[2], int(sys.argv[3]), int(sys.argv[4])
TOL = 0.0072
def warband(d):
    st = pe = 0; seeds = 0
    for sd in range(S0, S1 + 1):
        p = os.path.join(d, f'warband_s{sd}.tsv')
        if not os.path.exists(p): continue
        seeds += 1
        with open(p, encoding='utf-8') as f:
            h = f.readline().rstrip('\n').split('\t'); ix = {n: i for i, n in enumerate(h)}
            sc = [i for n, i in ix.items() if n.startswith('start_') and n != 'start_civil']
            for l in f:
                r = l.rstrip('\n').split('\t'); st += sum(int(r[i]) for i in sc); pe += int(r[ix['peaceResets']])
    return seeds, st, pe
nb, sb, pb = warband(OFF); na, sa, pa = warband(ARM)
base = sb / pb if pb > 0 else float('nan'); r4 = sa / pa if pa > 0 else float('nan')
lo, hi = base * (1 - TOL), base * (1 + TOL)
print(f'G4 基準（旗 0）開戦÷講和={base:.6f}（{sb}/{pb}・種 {nb}）')
print(f'G4 壊さない 開戦÷講和={r4:.6f}（{sa}/{pa}・種 {na}・幅 {lo:.6f}〜{hi:.6f}）→ {"緑" if (na == S1 - S0 + 1 and lo <= r4 <= hi) else "赤"}')
