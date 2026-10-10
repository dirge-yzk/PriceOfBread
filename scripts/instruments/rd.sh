#!/usr/bin/env bash
# (209) 計器の生の行から読み手を回す（読むだけ）。bash rd.sh <out_dir> <タグ>
# (245) 史実比較の桁（NG）の読み手 11 本（navy・rail・steam・fin・hist171・hist・sold・wardead・mil180・trade・warlaw）は標準の計器の出力を読む＝ng.sh へ移した（ここには残さない）。
#   mil180 を読む lawmatch はここに残る（$R/mil180_$T.txt は ng.sh が書く）。
export PYTHONUTF8=1
SP="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
O=$1; T=$2; R="${POB_WORK:?POB_WORK を指定}/rd"; mkdir -p $R
python $SP/s3a/an_band.py $O/s3a > $R/band_$T.txt 2>&1
python $SP/hol/an_h2.py $O/ghost -v4 > $R/hol_$T.txt 2>&1
python $SP/need159/an_q11d.py $O/disc $T > $R/q11_$T.txt 2>&1
python $SP/m160/an_need.py $O/need > $R/need_$T.txt 2>&1
python $SP/m170/an_mil170.py $T=$O/hist > $R/mil_$T.txt 2>&1
python $SP/m171/an_war171.py $O $T > $R/war_$T.txt 2>&1
python $SP/m171/an_casdec.py $O > $R/casdec_$T.txt 2>&1
python $SP/m177/an_col177.py $O/col > $R/col_$T.txt 2>&1
python $SP/m176/an_stage176.py $O $T > $R/stage_$T.txt 2>&1
python $SP/m176/an_fin176e.py $O $T > $R/fine_$T.txt 2>&1
python $SP/m176/an_treas176.py $O $T > $R/treas_$T.txt 2>&1
python $SP/m176/an_default176.py $O $T > $R/default_$T.txt 2>&1
python $SP/m176/an_nocontact176.py $O/w171 $T > $R/nocontact_$T.txt 2>&1
python $SP/m172/an_open172.py $O/w171 $T > $R/open_$T.txt 2>&1
python $SP/m169/an_site169.py ev $O/d165 $T > $R/site_$T.txt 2>&1
python $SP/m181/an_r181.py $O $T > $R/r181_$T.txt 2>&1
python $SP/m181/an_uk181.py $O $T > $R/uk_$T.txt 2>&1
python $SP/m183/an_navy183.py $O/navy > $R/navy183_$T.txt 2>&1
python $SP/m183/an_tier183.py $O/tier > $R/tier183_$T.txt 2>&1
python $SP/m183/an_ret183.py $O/tier > $R/ret183_$T.txt 2>&1
python $SP/m183/an_sec183.py $O/sec183 > $R/sec183_$T.txt 2>&1
python $SP/m183/an_need183.py $O/need183 1815,1860,1913 > $R/need183_$T.txt 2>&1
for f in $R/*_$T.txt; do echo "$(basename $f) $(wc -l < $f) $(grep -c -i 'Traceback\|Error' $f)"; done
python $SP/m184/an_serf184.py $O/serf184 > $R/serf184_$T.txt 2>&1
for f in $R/serf184_$T.txt; do echo "$(basename $f) $(wc -l < $f) $(grep -c -i "Traceback\|Error" $f)"; done
python $SP/m186/an_gold186.py $O/g186 > $R/gold_$T.txt 2>&1
for f in $R/trade_$T.txt $R/gold_$T.txt; do echo "$(basename $f) $(wc -l < $f) $(grep -c -i "Traceback\|Error" $f)"; done
python $SP/m190/an_nvy190.py $O $T > $R/nvy190_$T.txt 2>&1; echo "nvy190_$T.txt $(wc -l < $R/nvy190_$T.txt)"
python $SP/need/an_mig.py $O/mig > $R/mig_$T.txt 2>&1; echo "mig_$T.txt $(wc -l < $R/mig_$T.txt)"
python $SP/m192/an_lawmatch192.py $T=$R/mil180_$T.txt > $R/lawmatch_$T.txt 2>&1; echo "lawmatch_$T.txt $(wc -l < $R/lawmatch_$T.txt)"
python $SP/m193/an_cnl193.py $O/cnl > $R/cnl_$T.txt 2>&1; echo "cnl_$T.txt $(wc -l < $R/cnl_$T.txt)"
python $SP/m193/an_postwar193.py $O/cnl > $R/postwar_$T.txt 2>&1; echo "postwar_$T.txt $(wc -l < $R/postwar_$T.txt)"
python $SP/m194/an_mob194.py $O $T > $R/mob_$T.txt 2>&1; echo "mob_$T.txt $(wc -l < $R/mob_$T.txt)"
python $SP/m195/an_nvq195.py $O > $R/nvq_$T.txt 2>&1; echo "nvq_$T.txt $(wc -l < $R/nvq_$T.txt)"
python $SP/m197/an_osc197.py $O $T > $R/osc_$T.txt 2>&1; echo "osc_$T.txt $(wc -l < $R/osc_$T.txt)"
python $SP/m185/an_cap185.py $O/cap 10 > $R/cap_$T.txt 2>&1; echo "cap_$T.txt $(wc -l < $R/cap_$T.txt)"
python $SP/m185/an_cap185.py $O/cap 20 > $R/cap20_$T.txt 2>&1; echo "cap20_$T.txt $(wc -l < $R/cap20_$T.txt)"
python $SP/m199/an_cnl199.py $O $T > $R/cnl199_$T.txt 2>&1; echo "cnl199_$T.txt $(wc -l < $R/cnl199_$T.txt)"
python $SP/m201/an_chain201.py $O $T > $R/chain_$T.txt 2>&1; echo "chain_$T.txt $(wc -l < $R/chain_$T.txt)"
python $SP/m201/an_wars201.py $O $T > $R/wars_$T.txt 2>&1; echo "wars_$T.txt $(wc -l < $R/wars_$T.txt)"
python $SP/m203/an_t203.py $O $T > $R/t203_$T.txt 2>&1; echo "t203_$T.txt $(wc -l < $R/t203_$T.txt)"
python $SP/m204/an_d204.py $O $T > $R/d204_$T.txt 2>&1; echo "d204_$T.txt $(wc -l < $R/d204_$T.txt)"
python $SP/m204/an_rv204.py $O $T > $R/rv204_$T.txt 2>&1; echo "rv204_$T.txt $(wc -l < $R/rv204_$T.txt)"
python $SP/m204/an_wars204.py $O $T > $R/wars204_$T.txt 2>&1; echo "wars204_$T.txt $(wc -l < $R/wars204_$T.txt)"
python $SP/m205/an_fx205.py $O/fx205 $T > $R/fx205_$T.txt 2>&1; echo "fx205_$T.txt $(wc -l < $R/fx205_$T.txt)"
python $SP/m205/an_gold205.py $O > $R/gold205_$T.txt 2>&1; echo "gold205_$T.txt $(wc -l < $R/gold205_$T.txt)"
python $SP/m205/an_frt205.py $O > $R/frt205_$T.txt 2>&1; echo "frt205_$T.txt $(wc -l < $R/frt205_$T.txt)"
python $SP/m205/an_span205.py $O > $R/span205_$T.txt 2>&1; echo "span205_$T.txt $(wc -l < $R/span205_$T.txt)"
python $SP/m205/an_spain205.py $O > $R/spain205_$T.txt 2>&1; echo "spain205_$T.txt $(wc -l < $R/spain205_$T.txt)"
python $SP/m206/an_c206.py $O/c206 $T > $R/c206_$T.txt 2>&1; echo "c206_$T.txt $(wc -l < $R/c206_$T.txt)"
python $SP/m208/an_s208.py $O/s208 $T > $R/s208_$T.txt 2>&1; echo "s208_$T.txt $(wc -l < $R/s208_$T.txt)"
python $SP/m208/an_ind208.py $O/s208 $T > $R/ind208_$T.txt 2>&1; echo "ind208_$T.txt $(wc -l < $R/ind208_$T.txt)"
python $SP/m207/an_d207.py $O $T > $R/d207_$T.txt 2>&1; echo "d207_$T.txt $(wc -l < $R/d207_$T.txt)"
