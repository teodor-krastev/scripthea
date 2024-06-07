import sys, os

st.print(sys.executable)
st.print(sys.base_prefix)
st.print(sys.prefix)
st.print("----------------")
for path in sys.path:
	st.print(path)


if os.environ.get('VIRTUAL_ENV'):
    st.print("Running inside a virtual environment")
else:
    st.print("Not running inside a virtual environment")