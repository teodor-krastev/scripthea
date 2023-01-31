import copy
import math
import os
import random
import sys
import traceback
import shlex
import time

import msvcrt
import json

import modules.scripts as scripts
import gradio as gr

from modules.processing import Processed, process_images
from PIL import Image
from modules.shared import opts, cmd_opts, state
import modules.images as simages


# MIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIINE
named_pipe2s = "\\\\.\\pipe\\scripthea_pipe2s"
named_pipe2c = "\\\\.\\pipe\\scripthea_pipe2c"

debugComm = True
def dprint(txt):
    if debugComm:
        print(txt)

def isPipeOpen(named_pipe):
    return os.path.exists(named_pipe) and os.access(named_pipe, os.R_OK | os.W_OK)

def wait4server():
    timeOut = 12 # two min
    global pipe2s
    global pipe2c
    while (timeOut > 0):
        try:
            pipe2s = os.open(named_pipe2s, os.O_WRONLY)
            pipe2c = os.open(named_pipe2c, os.O_RDONLY)
            break
        except:
            time.sleep(10)
            dprint('comm attempts left: '+str(timeOut))
            timeOut -= 1
    return (timeOut > 0)
def OneShot():
    try:
        message = '@next.prompt\n'
        os.write(pipe2s, message.encode())
        #dprint('out: '+message)
        inData = os.read(pipe2c, 4096).decode().strip()
        #dprint('in: '+inData)
    except:
        return '@close.session'
    return inData

# miiiiiiiiiiiiiiiiiiiiiiiiiiiiine

def process_string_tag(tag):
    return tag


def process_int_tag(tag):
    return int(tag)


def process_float_tag(tag):
    return float(tag)


def process_boolean_tag(tag):
    return True if (tag == "true") else False


prompt_tags = {
    "sd_model": None,
    "outpath_samples": process_string_tag,
    "outpath_grids": process_string_tag,
    "prompt_for_display": process_string_tag,
    "prompt": process_string_tag,
    "negative_prompt": process_string_tag,
    "styles": process_string_tag,
    "seed": process_int_tag,
    "subseed_strength": process_float_tag,
    "subseed": process_int_tag,
    "seed_resize_from_h": process_int_tag,
    "seed_resize_from_w": process_int_tag,
    "sampler_index": process_int_tag,
    "batch_size": process_int_tag,
    "n_iter": process_int_tag,
    "steps": process_int_tag,
    "cfg_scale": process_float_tag,
    "width": process_int_tag,
    "height": process_int_tag,
    "restore_faces": process_boolean_tag,
    "tiling": process_boolean_tag,
    "do_not_save_samples": process_boolean_tag,
    "do_not_save_grid": process_boolean_tag
}

def cmdargs(line):
    args = shlex.split(line)
    pos = 0
    res = {}

    while pos < len(args):
        arg = args[pos]

        assert arg.startswith("--"), f'must start with "--": {arg}'
        tag = arg[2:]

        func = prompt_tags.get(tag, None)
        assert func, f'unknown commandline option: {arg}'

        assert pos+1 < len(args), f'missing argument for command line option {arg}'

        val = args[pos+1]

        res[tag] = func(val)

        pos += 2

    return res


def load_prompt_file(file):
    if file is None:
        lines = []
    else:
        lines = [x.strip() for x in file.decode('utf8', errors='ignore').split("\n")]

    return None, "\n".join(lines), gr.update(lines=7)


class Script(scripts.Script):
    def title(self):
        return "Prompts from Scripthea (v1.5)"

    def ui(self, is_img2img):
        return []

    def run(self, p):
        if not wait4server():
            dprint('TIME OUT !')
            return
        dprint('Scripthea session started')

        images = []
        all_prompts = []
        infotexts = []
        while True:
            inData = OneShot()
            dprint('+>'+inData)
            if (inData.lower().strip() == '@close.session'):
                break
            jsn = json.loads(inData)
            prom = jsn["prompt"]

            if "--" in prom:
                try:
                    args = cmdargs(prom)
                except Exception:
                    print(f"Error parsing line [prom] as commandline:", file=sys.stderr)
                    print(traceback.format_exc(), file=sys.stderr)
                    args = {"prompt": prom}
            else:
                args = {"prompt": prom}

            copy_p = copy.copy(p)
            for k, v in args.items():
                setattr(copy_p, k, v)

            proc = process_images(copy_p)
            images += proc.images
            all_prompts += proc.all_prompts
            infotexts += proc.infotexts

            if len(proc.images) > 0:
                if (jsn["filename"] != ""):
                    simages.save_image(proc.images[0], jsn["folder"],"",prompt = prom,info = proc.infotexts[0], p=p,  forced_filename = jsn["filename"])
                time.sleep(1)
                message = '@image.ready\n'
            else:
                message = '@image.failed\n'

            os.write(pipe2s, message.encode())
            time.sleep(2)

        # close the connection
        if isPipeOpen(named_pipe2s):
            os.close(pipe2s)
        if isPipeOpen(named_pipe2c):
            os.close(pipe2c)
        dprint('Scripthea session closed')

        return Processed(p, images, p.seed, "", all_prompts=all_prompts, infotexts=infotexts)
