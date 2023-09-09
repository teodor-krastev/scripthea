#result = query.stScanImages()
for a in range(600):
	if st.CancelRequest():
		break
	st.print(a)
result = sdPrms.stSet("negative_prompt","very negative")