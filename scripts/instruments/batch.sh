#!/usr/bin/env bash
# ★旧式（注入）の計器の一式の入口＝この一式は m241 までの回で使った写し（2026-10-10 に scratchpad から scripts/instruments/ へ移した）。
#   新しい読むだけの標準の計器（env POB_INSTRUMENTS・SimCore/Instruments.cs）は注入が要らない。
#   (245) 史実比較の桁（NG）の読み手の入力 11 種（navy・rail・steam・fin・hist171・hist・sold・wardead・mil180・trade・warlaw）は標準の計器へ畳んだ
#   ＝`INS=1 bash batch.sh run <腕>` が `POB_INSTRUMENTS=<出力>/ins` を立てて回し、`bash ng.sh <出力>/ins <タグ> <読み手の出力フォルダ>` が読む（注入は要らない）。
#   ここに残る fullS の注入は NG の入力ではない（rd.sh の他の読み手と、過去の回の再現のため）。単独の読み手しか無かった gate・wpn・t186 の注入は消した。
#   ⚠注入は木のソースの形に依存する（assert で止まる）＝HEAD で通るか先に確かめる。
#   ⚠⚠(248) **(246) 以後の木（専用の値型 MoneyPerMonth・RatePerYear ほか）では fullS・probe・fullX の注入は当たらない**＝錨が消えた（army/inj_fc3・mil 連鎖〔m194/inj_mil8x→…→mil2/inj_mil4〕・m190/fix_mil4）・
#     注入した式が double と型を混ぜてコンパイルが通らない（m204/inj_d204・m205/inj_fx205 ほか約40件）。計器つきの帯は `INS=1 … run`（標準の計器）で回す。
#     注入の読み手を使う回は、その量を標準の計器（SimCore/Instruments.cs）へ畳んでから読む（注入を直さない）。
#   環境変数：POB_WORK＝作業フォルダ（必須・D: 側）／POB_REPO＝リポジトリ（既定＝この scripts/instruments の2つ上）
# (209) 線S：写しの木＋読むだけの計器で measure.sh を1本（前面・1本ずつ・腕ごとに基線の写し）
#   bash batch.sh prep <腕> <commit:<sha>|dir:<path>> <計器 fullS|probe|plain> [編集の python（パス・引数に木の根）...]
#      fullS＝(207) の fullX＋m208/inj_s208（金本位の波の入力）／probe＝s3b/inj_s3a＋u4e/inj_ghost＋m205/inj_fx205＋m208/inj_s208／plain＝計器なし
#      fullX＝(202) の fullM＋m205/inj_fx205（英の対外資産と運賃・為替）＋m206/inj_c206（海の必要量の内訳）＋m203/inj_t203（潤沢の減税と税の段）＋m204/inj_d204（不履行の判定の鎖）＋m204/inj_rv204（蜂起の漏斗）／plain＝計器なし
#   bash batch.sh run <腕> [種の数=10] [種の起点=1]
export AVALONIA_TELEMETRY_OPTOUT=1
export PATH="/usr/bin:/c/Program Files/Git/usr/bin:/c/Program Files/Git/cmd:/c/Program Files/dotnet:$PATH"
export PYTHONUTF8=1
SP="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO="${POB_REPO:-$(cd "$SP/../.." && pwd)}"
W="${POB_WORK:?POB_WORK（作業フォルダ）を指定してください}"; ARM=$2; SRC=$W/src_$ARM
if [ "$1" = prep ]; then
  FROM=$3; INST=$4; shift 4
  rm -rf $SRC && mkdir -p $SRC
  case "$FROM" in
    dir:*) tar -cf - -C ${FROM#dir:} --exclude=bin --exclude=obj --exclude=.git --exclude=docs --exclude=measurements --exclude=uishot --exclude=.vs . | tar -xf - -C $SRC || exit 1 ;;
    commit:*) (cd $REPO && git archive ${FROM#commit:}) | tar -x -C $SRC || exit 1 ;;
  esac
  for e in "$@"; do python $e $SRC || { echo "EDITFAIL $e"; exit 1; }; done
  case "$INST" in
    probe)
      for inj in s3b/inj_s3a.py u4e/inj_ghost.py m205/inj_fx205.py m208/inj_s208.py; do
        python $SP/$inj $SRC > /dev/null || { echo "INJFAIL $inj"; exit 1; }
      done ;;
    fullS)
      for inj in s3b/inj_s3a.py u4e/inj_ghost.py m149/inj_disc.py m190/inj_navy_n.py m160/inj_need.py army/inj_fc3.py m194/inj_mil8x.py m190/fix_mil4.py; do
        python $SP/$inj $SRC > /dev/null || { echo "INJFAIL $inj"; exit 1; }
      done
      for inj in cas156/inj_cas.py m168/inj_d165.py m190/inj_war171n.py m177/inj_col177.py m181/inj_war181.py m207/inj_fin181y.py m183/inj_tier.py m195/inj_need183x.py m188/inj_sec188.py m184/inj_serf184.py m186/inj_g186.py m201/inj_c201.py m205/inj_fx205.py m190/inj_nvy190n.py need/inj_mig.py m199/inj_cnl199.py m194/inj_mob194.py m195/inj_nvq195.py m178/inj_pow178.py m185/inj_cap185.py m206/inj_c206.py m203/inj_t203.py m204/inj_d204.py m204/inj_rv204.py m208/inj_s208.py; do
        python $SP/$inj $SRC > /dev/null || { echo "INJFAIL $inj"; exit 1; }
      done ;;
    plain) ;;
    *) echo "BADINST $INST"; exit 1 ;;
  esac
  echo PREPDONE $ARM $FROM $INST "$@"
elif [ "$1" = run ]; then
  N=${3:-10}; SB=${4:-1}; TAG=${ARM}_$N; XARGS=""
  if [ "$SB" != "1" ]; then TAG=${ARM}_${N}_s$SB; XARGS="$N - 0 $SB"; fi
  O=$W/out_$TAG
  rm -rf $O $W/snap_$TAG && mkdir -p $O/s3a $O/ghost $O/disc $O/navy $O/need $O/hist $O/cas $O/d165 $O/w171 $O/col $O/w176 $O/f181 $O/tier $O/need183 $O/sec183 $O/serf184 $O/g186 $O/nvy190 $O/mig $O/cnl $O/mob $O/nvq $O/pow $O/cap $O/l186 $O/c201 $O/fx205 $O/c206 $O/t203 $O/d204 $O/rv204 $O/s208
  (cd $REPO && git show 1a3c227:.pob-fidelity-baseline.tsv) > $W/fid_$TAG.tsv
  echo "START $(date +%T) selfplay-batch N=$N SRC=$SRC" > $W/ms_$TAG.log
  export POB_COEF="$(cat $W/coef_$ARM.txt 2>/dev/null)"
  if [ -n "${INS:-}" ]; then mkdir -p $O/ins; export POB_INSTRUMENTS=$O/ins; fi   # (245) 標準の計器（NG の入力）を出す＝注入なしの木（plain）で回す
  SEEDS=$N SNAP_DIR=$W/snap_$TAG POB_FIDELITY_BASELINE=$W/fid_$TAG.tsv POB_S3A_DIR=$O/s3a POB_GHOST_DIR=$O/ghost POB_DISC_DIR=$O/disc POB_NAVY_DIR=$O/navy POB_NEED_DIR=$O/need POB_FC_DIR=$O/hist POB_MIL_DIR=$O/hist POB_CAS_DIR=$O/cas POB_D165_DIR=$O/d165 POB_W171_DIR=$O/w171 POB_COL175_DIR=$O/col POB_W176_DIR=$O/w176 POB_F181_DIR=$O/f181 POB_TIER_DIR=$O/tier POB_NEED183_DIR=$O/need183 POB_SEC183_DIR=$O/sec183 POB_SERF184_DIR=$O/serf184 POB_G186_DIR=$O/g186 POB_NVY190_DIR=$O/nvy190 POB_MIG_DIR=$O/mig POB_CNL193_DIR=$O/cnl POB_MOB194_DIR=$O/mob POB_NVQ195_DIR=$O/nvq POB_POW_DIR=$O/pow POB_CAP_DIR=$O/cap POB_L186_DIR=$O/l186 POB_C201_DIR=$O/c201 POB_FX205_DIR=$O/fx205 POB_C206_DIR=$O/c206 POB_T203_DIR=$O/t203 POB_D204_DIR=$O/d204 POB_RV204_DIR=$O/rv204 POB_S208_DIR=$O/s208 \
    bash $SRC/measure.sh $W/b${N}_$TAG.txt selfplay-batch $XARGS >> $W/ms_$TAG.log 2>&1
  echo "rc=$? END $(date +%T)" >> $W/ms_$TAG.log
  rm -rf $W/snap_$TAG
  tail -4 $W/ms_$TAG.log
  echo BATCHDONE $TAG
fi
