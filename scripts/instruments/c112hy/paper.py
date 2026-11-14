# (1145) c112hy 紙の台本（今の盤面＝keep_1143 の生だけ・読むだけ・近似）。
# 近似：四半期の末の断面（warpair_s の緊張と遺恨・scalars_s の敗戦の衝撃と厭戦）で、月ごとの枝を代わりに読む。従属（凍結）の組は数えない。
# 出す物：
#   P1 講和の直後（遺恨≧0.9）の高い帯の行のうちハイエナの枝が先に取る割合（r1141/grudge_mask.py と同じ近似）
#   P2 遺恨のある高い帯の行の、旧の規則の月の冷めの量の平均（ハイエナ 2.0×衝撃／他 0.42×(1−遺恨)）と新の規則の平均（0.42×(1−遺恨)）
#   P3 開戦の数・講和の数・比（warband_s）
#   P4 講和の後 8 四半期以内に同じ組が再び開戦した割合（warpair_s の peaceResets と starts）
#   P5 朝鮮×清の屈服の回数（warpair_s submits・種ごと）
#   P6 講和の直後の高い帯の行のうち、どちらかの国の厭戦が門（0.5）を超える割合
# 使い方：python -I scripts/instruments/c112hy/paper.py <生のフォルダ> <ラベル> [種の始め 1] [種の終わり 10]
import sys, os
from collections import defaultdict
D, LAB = sys.argv[1], sys.argv[2]
S0 = int(sys.argv[3]) if len(sys.argv) > 3 else 1
S1 = int(sys.argv[4]) if len(sys.argv) > 4 else 10
BAND, MIN, RELIEF, DECAY, WEARY = 100.0, 0.3, 2.0, 0.42, 0.5
g = fresh = fresh_hy = fresh_weary = 0; old_sum = new_sum = 0.0
st = pe = 0; peace_ev = rewar = 0; kq = []
for s in range(S0, S1 + 1):
    sh = {}; wx = {}
    with open(os.path.join(D, f'scalars_s{s}.tsv'), encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {n: i for i, n in enumerate(h)}
        for l in f:
            r = l.rstrip('\n').split('\t'); k = (int(r[0]), r[ix['nation']])
            sh[k] = float(r[ix['defeatShock']]); wx[k] = float(r[ix['warExhaustion']])
    with open(os.path.join(D, f'warband_s{s}.tsv'), encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {n: i for i, n in enumerate(h)}
        sc = [i for n, i in ix.items() if n.startswith('start_') and n != 'start_civil']
        for l in f:
            r = l.rstrip('\n').split('\t'); st += sum(int(r[i]) for i in sc); pe += int(r[ix['peaceResets']])
    ev = defaultdict(list); sub_kq = 0
    with open(os.path.join(D, f'warpair_s{s}.tsv'), encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {n: i for i, n in enumerate(h)}
        for l in f:
            r = l.rstrip('\n').split('\t')
            q = int(r[0]); a, b = r[1], r[2]; pair = tuple(sorted((a, b)))
            if int(r[ix['peaceResets']]) > 0: ev[pair].append((q, 'p'))
            if int(r[ix['starts']]) > 0: ev[pair].append((q, 's'))
            if set(pair) == {'朝鮮', '清'}: sub_kq += int(r[ix['submits']])
            t = float(r[ix['tensionEnd']]); gr = float(r[ix['grudge']])
            if not (t >= BAND) or not (gr > 0): continue
            xa, xb = sh.get((q, a)), sh.get((q, b))
            if xa is None and xb is None: continue
            m = max(x for x in (xa, xb) if x is not None)
            gg = min(1.0, gr)
            new = DECAY * (1 - gg); old = RELIEF * m if m >= MIN else new
            g += 1; old_sum += old; new_sum += new
            if gr >= 0.9:
                fresh += 1
                if m >= MIN: fresh_hy += 1
                wa, wb = wx.get((q, a), 0.0), wx.get((q, b), 0.0)
                if max(wa, wb) > WEARY: fresh_weary += 1
    for pair, es in ev.items():
        ps = sorted(q for q, k in es if k == 'p'); ss = sorted(q for q, k in es if k == 's')
        for qp in ps:
            peace_ev += 1
            if any(qp < qs <= qp + 8 for qs in ss): rewar += 1
    kq.append(sub_kq)
print(f'{LAB}\tP1_freshHyenaShare={fresh_hy/max(fresh,1):.4f}\t({fresh_hy}/{fresh})\tP2_oldMeanCool={old_sum/max(g,1):.6f}\tnewMeanCool={new_sum/max(g,1):.6f}\trows={g}'
      f'\tP3_starts={st}\tpeace={pe}\tratio={st/max(pe,1):.6f}\tP4_rewar8q={rewar/max(peace_ev,1):.4f}\t({rewar}/{peace_ev})'
      f'\tP5_koreaQingSubmits={sum(kq)}\tper_seed={",".join(map(str, kq))}\tP6_freshWearyShare={fresh_weary/max(fresh,1):.4f}')
