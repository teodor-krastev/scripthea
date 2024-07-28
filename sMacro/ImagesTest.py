import sys
# Image generation sMacro

# single image generation
st.log("out: "+qry.Text2Image('Desert sunrise by Turner'))

# select some cues, 10% of the whole pool
lr = qry.SelectCues(3, -1)
st.log(str(lr.Count)+' cues selected')

# select some modifiers
qry.mSetApply('mSet.1', False)

# generate prompt from selected cues and modifiers
ls = qry.GetPreview(False)
st.log(str(ls.Count)+' promts generated in preview')

if (st.IsCancellationRequested()):
	sys.exit(1)

# generate images from list of prompts
lt = qry.PromptList2Image(ls)
st.log(str(lt.Count)+' images generated')

