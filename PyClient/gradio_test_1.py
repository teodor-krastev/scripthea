#-------------------------------------------------------------------------------
# Name:        module1
# Purpose:
#
# Author:      Theo
#
# Created:     16/01/2023
# Copyright:   (c) User 2023
# Licence:     <your licence>
#-------------------------------------------------------------------------------
import gradio as gr

def hello():
    return "> theo"

def main():
    # demo = gr.Interface.load("noplace/noname", title="title").launch()
    txt = gr.Textbox(label="List of prompt inputs", lines=7, value = "XXXXXXXX\n")
    txt.value += "YYYYYYY"
    print(txt.value)

if __name__ == '__main__':
    main()
