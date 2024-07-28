data = [1, 2, 3, 'hello', (4, 5)]
# write left - user msg
st.log(data)
# write right - debug msg
st.print(data,'Blue') 
print(data)

# input
s = st.Input('Write something', 'anything')
st.print(s,'Green') 

# set & get image width
prm.set('width', 750)

print(prm.get('width'))

# generate error
print(1/0)

