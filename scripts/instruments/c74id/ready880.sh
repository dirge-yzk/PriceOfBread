#!/usr/bin/env bash
# (880) c74id 印 id_ready.mark の中身：.log の先頭に事前登録の sha256・(b) の書き物のパス・(d) の決めたことを書き、この枝の木（旗 0）で走査の番人 7 組と線の試験の組を回して rc を返す（0＝c74ip が merge してよい）。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/c74id_work/id_ready.mark bash scripts/instruments/c74id/ready880.sh
#   旗 1（ImportPayerLive＝1・SubsistenceNoBoughtTech＝1）の使い捨ての木の結果は measurements/m880_20261106/guards_flag1_*.txt（この台本の前に回した）。
export PATH="/usr/bin:/c/Program Files/Git/usr/bin:/c/Program Files/Git/cmd:/c/Program Files/dotnet:$PATH"
SP="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"; REPO="$(cd "$SP/../../.." && pwd)"
cd "$REPO" || exit 9
echo "PREREG measurements/m880_20261106_prereg_id.txt sha256 $(sha256sum measurements/m880_20261106_prereg_id.txt | cut -d' ' -f1)"
echo "U2S measurements/m880_20261106/u2s_source880.txt（門 U2L の形＝cearn_s の末尾の列 importBudgetExcessMaxPerClearingSubLive）"
echo "HOLOWN scripts/instruments/c74id/holown880.py（R880＝A2 ∪ 持ち主の照合・OR 形・自己試験 measurements/m880_20261106/selftest880.txt・ヌル腕 null880.txt）"
echo "ROUTEKM (i) 盤面の選び直しを入れた＝for の四半期 40→60（Assert は不変）・旗 0・ImportPayerLive＝1・SubsistenceNoBoughtTech＝1 とも緑（measurements/m880_20261106/routekm880.txt）＝c74ip は RouteKmTests を予言する赤に書かなくてよい"
echo "HEAD $(git rev-parse HEAD)  origin/c74id $(git rev-parse origin/c74id 2>/dev/null)"
bash "$SP/guards880.sh" "$REPO" D:/PoB_work/c74id_work/art_g0 all
