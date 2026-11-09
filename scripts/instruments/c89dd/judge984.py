# (984) c89dd 門の判定（読むだけ）：gate984.py の出力の〈計〉の行を、旗 0（基準）と並べて事前登録 [1] の G1a・G1b・G3 を判定する。
#   python -X utf8 -I judge984.py <腕の gate984 出力> <基準の gate984 出力> [<ヌル腕の gate984 出力>...]
#   G1a：塵の割合 ≦ 基準の 1/10・G1b：billBlockedDust の和 ≦ 基準の 1/10・G3：debt ＜0 の行＝0。記録：植民地格の輸入の払いの新÷旧とヌル腕の幅。
import sys
def tot(p):
    with open(p, encoding='utf-8') as f:
        for line in f:
            if line.startswith('seed\t'): h = line.rstrip('\n').split('\t')
            if line.startswith('計\t'): x = line.rstrip('\n').split('\t'); return {k: x[i] for i, k in enumerate(h) if i > 0}
    raise SystemExit(f'計の行が無い：{p}')
a, b = tot(sys.argv[1]), tot(sys.argv[2]); nulls = [tot(p) for p in sys.argv[3:]]
sa, sb = float(a['dustShare']), float(b['dustShare']); da, db = int(a['billBlockedDust']), int(b['billBlockedDust']); neg = int(a['negDebtRows'])
g1a = sa <= sb / 10; g1b = da <= db / 10; g3 = neg == 0
print(f'G1a 塵の割合 腕 {sa:.6f}・基準 {sb:.6f}・比 {sa / sb:.4f}・門 ≦{sb / 10:.6f} → {"通る" if g1a else "落ちる"}')
print(f'G1b billBlockedDust 腕 {da}・基準 {db}・比 {da / db:.4f}・門 ≦{db / 10:.1f} → {"通る" if g1b else "落ちる"}')
print(f'G3 debt＜0 の行 腕 {neg} → {"通る" if g3 else "落ちる"}')
ipa, ipb = float(a['importPaidColonial']), float(b['importPaidColonial'])
w = [float(n['importPaidColonial']) / ipb for n in nulls]
print(f'記録 植民地格の輸入の払い 新÷旧 {ipa / ipb:.4f}・ヌル腕 {", ".join(f"{x:.4f}" for x in w)}')
print(f'記録 本国格の輸入の払い 新÷旧 {float(a["importPaidCore"]) / float(b["importPaidCore"]):.4f}・billBlocked 新÷旧 {int(a["billBlocked"]) / int(b["billBlocked"]):.4f}・savings＜−1e-9 の行 {a["negSavRows"]}（基準 {b["negSavRows"]}）・塵の続き {a["dustRuns"]}（基準 {b["dustRuns"]}）')
print('G1・G3 ' + ('通る' if g1a and g1b and g3 else '落ちる'))
sys.exit(0 if g1a and g1b and g3 else 1)
