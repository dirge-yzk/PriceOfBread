# (1253) dk の予備計測の規則（(461) ③：d＝14 の行が 90 パーセンタイルの 0.8 倍で S＝1）を今の基準の W と供給の能力に当てる（読むだけ）
import json, statistics as st
def pct(v, p):
    v = sorted(v); k = (len(v) - 1) * p; i = int(k); return v[i] + (v[min(i + 1, len(v) - 1)] - v[i]) * (k - i)
B = {int(k): v for k, v in json.loads(open('data/base.json', 'rb').read().decode('utf-8')).items()}
B2 = {int(k): v for k, v in json.loads(open('data/base2.json', 'rb').read().decode('utf-8')).items()}
W = [x for s in B.values() for x in s['W'].values()]
S = [x for s in B2.values() for q, x in s['SUP'].items() if int(q) % 8 == 0]
for nm, v in (('W', W), ('供給の能力', S)):
    hi = pct(v, 0.9); c = 0.8 * hi / (14 * 1.05)
    print(f'{nm}：90 パーセンタイル {hi:.0f}・最大 {max(v):.0f}・規則の定数 {c:.1f}')
print('今の W0 7854.08 で S＝1 に要る W（d＝12・13・14）：', ' '.join(f'{7854.08*d*1.05:.0f}' for d in (12, 13, 14)))
print('基準の W の最大（種ごと）：', ' '.join(f'{max(s["W"].values()):.0f}' for s in B.values()))
