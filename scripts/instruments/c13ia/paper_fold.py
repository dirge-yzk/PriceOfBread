# (811) c13ia 紙の台本：旗の焼き込みが条件の枝の削除だけであることを、Simulation.cs の旧と新で示す。
# 方法＝旧と新の両方からコメント・空白・波括弧を除き、旧に〈宣言した折りたたみ 7 組〉だけを当てた物が新と一致すること。
# 式の本体（演算子・項の順・定数）が 1 文字でも違えば不一致になる。使い方: python paper_fold.py <旧のファイル> <新のファイル>
import re, sys
def norm(s):
    s = re.sub(r'/\*.*?\*/', '', s, flags=re.S)
    s = re.sub(r'//[^\n]*', '', s)
    s = re.sub(r'\s+', '', s)
    return s.replace('{', '').replace('}', '')
FOLDS = [   # 旧の文字列（空白・括弧除去後）→ 新
 ('if(coef.MachineCastIronRecipe>0)', ''),
 ('if(coef.KeroseneFromOilEnabled>0)', ''),
 ('if(coef.ChilledMeatEnabled>0)', ''),
 ('boolexColonies=c.RegionalPriceBaseExcludesColonies>0;', ''),
 ('exColonies&&', ''),
 ('if(World.Coef.MilitaryBudgetNeedCapped>0&&r.Id==0)', 'if(r.Id==0)'),
 ('if(World.Coef.MilitaryBudgetNeedCapped>0)navalNat=', 'navalNat='),
 ('(World.EducationLaw==EducationLaw.Religious&&World.Coef.ParishEduFunded>0)', 'World.EducationLaw==EducationLaw.Religious'),
 ('if(!(coef.HouseholdProductionEnabled>0))return;', ''),
]
old = norm(open(sys.argv[1], 'rb').read().decode('utf-8'))
new = norm(open(sys.argv[2], 'rb').read().decode('utf-8'))
for a, b in FOLDS:
    c = old.count(a)
    print(f'{c} 件 {a[:60]}')
    assert c >= 1, a
    old = old.replace(a, b)
if old == new:
    print('一致：旧に折りたたみを当てた物＝新（式の本体の違い 0）')
else:
    i = next((k for k in range(min(len(old), len(new))) if old[k] != new[k]), min(len(old), len(new)))
    print('不一致 位置', i); print('旧:', old[max(0, i-80):i+80]); print('新:', new[max(0, i-80):i+80]); sys.exit(1)
