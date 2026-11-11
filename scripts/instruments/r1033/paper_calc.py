# (1033) 紙の計算：EconomyStep.cs:1080〜1086 の work の式に重みを入れたときの値（読むだけ・エンジンを呼ばない）
# 年齢の割合はフランス 1783 の age_s1（15〜64＝0.591・65 以上＝0.049・10〜14＝0.104）・男女は半々と置く（AI の仮定）
A, O, C = 0.591, 0.049, 0.104
OLD = 0.75  # Coefficients.cs:559 OldLaborFactor
def work(part, clr, wf, wc, militia_male_loss=0.0):
    men = A / 2 * (1 - militia_male_loss); wom = A / 2
    return men + wom * part * wf + OLD * (O / 2 + O / 2 * part * wf) + C * wc * clr
def mix(hand, mach, m): return hand + (mach - hand) * m
cases = {'今の式(女1・子0.5)': (1.0, 0.5, 1.0, 0.5), '手0.6/0.35・機0.95/0.8': (0.6, 0.35, 0.95, 0.8),
         '手0.7/0.5・機0.95/0.8': (0.7, 0.5, 0.95, 0.8), '手0.5/0.25・機0.95/0.8': (0.5, 0.25, 0.95, 0.8)}
clr = 0.93
print('## t=0 の work（参加 0.4・児童労働の割合 0.93）と、機械の割合 m ごとの新÷(m=0)')
for k, (fh, ch, fm, cm) in cases.items():
    w0 = work(0.4, clr, fh, ch)
    row = ' '.join(f'm={m}:{work(0.4, clr, mix(fh, fm, m), mix(ch, cm, m)) / w0:.3f}' for m in (0.06, 0.4, 0.65, 0.9, 1.0))
    print(f'{k}: work(m=0)={w0:.4f} ' + row)
print('## 衝撃の大きさ（m を固定・新÷旧の work）')
for k, (fh, ch, fm, cm) in cases.items():
    for m in (0.0, 0.9):
        wf, wc = mix(fh, fm, m), mix(ch, cm, m); b = work(0.4, clr, wf, wc)
        prop = work(0.7, clr, wf, wc) / b - 1; ban = work(0.4, 0.0, wf, wc) / b - 1; war = work(0.4, clr, wf, wc, 0.10) / b - 1
        print(f'{k} m={m}: 財産権0.4→0.7 {prop:+.3f}・児童労働の禁止 {ban:+.3f}・15〜64歳の男の1割を失う {war:+.3f}')
