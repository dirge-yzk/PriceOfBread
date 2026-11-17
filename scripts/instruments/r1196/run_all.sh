python -X utf8 -I scripts/q1_form.py D:/PoB_work/keep_1191/base_ins 1 10 C:/Users/dirge/Documents/PriceOfBread/PriceOfBread.SimCore/world.json q1/form_m1191_s1_10 > q1/form_m1191_s1_10.txt 2>&1
python -X utf8 -I scripts/q1_form.py D:/PoB_work/keep_1184/base_ins 1 10 C:/Users/dirge/Documents/PriceOfBread/PriceOfBread.SimCore/world.json q1/form_m1184_s1_10 > q1/form_m1184_s1_10.txt 2>&1
python -X utf8 -I scripts/q1_form.py D:/PoB_work/keep_1191/a11 11 20 C:/Users/dirge/Documents/PriceOfBread/PriceOfBread.SimCore/world.json q1/form_m1191_s11_20 > q1/form_m1191_s11_20.txt 2>&1
python -X utf8 -I scripts/q1_form.py D:/PoB_work/keep_1191/n1 1 10 C:/Users/dirge/Documents/PriceOfBread/PriceOfBread.SimCore/world.json q1/form_n1_s1_10 > q1/form_n1_s1_10.txt 2>&1
echo rc=$? > q1/form_all.done
